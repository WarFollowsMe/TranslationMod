using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MultiLanguage
{
    public class FuzzyStringDictionary : IEnumerable<KeyValuePair<string, string>>
    {
        private Dictionary<string, string> _simpleDictionary;
        private Dictionary<string, string> _fuzzyDictionary;
        private OrderedDictionary _memoryBuffer;

        public FuzzyStringDictionary()
        {
            _simpleDictionary = new Dictionary<string, string>();
            _fuzzyDictionary = new Dictionary<string, string>();
            _memoryBuffer = new OrderedDictionary();
        }

        public string this[string key]
        {
            get
            {
                if (_simpleDictionary.ContainsKey(key))
                    return _simpleDictionary[key];
                else
                {
                    var compareKey = CheckInMemory(key);
                    if (string.IsNullOrEmpty(compareKey))
                    {
                        compareKey = CompareKey(key);
                        AddToMemory(key, compareKey);
                    }
                    return _fuzzyDictionary[compareKey];
                }
            }
            set
            {
                if (_simpleDictionary.ContainsKey(key))
                {
                    _simpleDictionary[key] = value;
                }
                else _fuzzyDictionary[CompareKey(key)] = value;
            }
        }

        public int Count
        {
            get
            {
                return _fuzzyDictionary.Count + _simpleDictionary.Count;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                return _fuzzyDictionary.Keys.Concat(_simpleDictionary.Keys);
            }
        }

        public IEnumerable<string> Values
        {
            get
            {
                return _fuzzyDictionary.Values.Concat(_simpleDictionary.Values);
            }
        }

        public void Add(string key, string value)
        {
            try
            {
                if (!key.Contains("@"))
                {
                    _simpleDictionary.Add(key, value);
                }
                else _fuzzyDictionary.Add(key, value);
            }
            catch (ArgumentException e) { }
        }

        public void AddRange(IEnumerable<KeyValuePair<string, string>> pairs)
        {
            foreach (var pair in pairs)
            {
                try
                {
                    if (!pair.Key.Contains("@"))
                    {
                        _simpleDictionary.Add(pair.Key, pair.Value);
                    }
                    else _fuzzyDictionary.Add(pair.Key, pair.Value);
                }
                catch (ArgumentException e) { }
            }
        }

        public void Clear()
        {
            _simpleDictionary.Clear();
            _fuzzyDictionary.Clear();
        }

        public bool ContainsKey(string key)
        {
            if (_simpleDictionary.ContainsKey(key) ||
                (key.First() == '\'' && key.Last() == '\'' && _simpleDictionary.ContainsKey(key.Trim('\''))))
                return true;

            var value = CompareKey(key);
            if (string.IsNullOrEmpty(value))
                return false;
            else
            {
                AddToMemory(key, value);
                return true;
            }
        }

        public bool Remove(string key)
        {
            bool result = false;

            if (_simpleDictionary.ContainsKey(key))
                result = _simpleDictionary.Remove(key);
            else
            {
                var removeItem = CompareKey(key);
                if (string.IsNullOrEmpty(removeItem))
                    result = false;
                else
                    result = _fuzzyDictionary.Remove(removeItem);
            }

            if (!string.IsNullOrEmpty(CheckInMemory(key)))
                _memoryBuffer.Remove(key);

            return result;
        }

        public KeyValuePair<string, string> GetFuzzyKeyValue(string key)
        {
            var fuzzyKey = "";
            var value = "";
            if (_simpleDictionary.ContainsKey(key))
            {
                fuzzyKey = key;
                value = _simpleDictionary[key];
            }
            else if (key.First() == '\'' && key.Last() == '\'' && _simpleDictionary.ContainsKey(key.Trim('\'')))
            {
                fuzzyKey = key;
                value = "'" + _simpleDictionary[key.Trim('\'')] + "'";
            }
            else
            {
                var memoryKey = CheckInMemory(key);
                if (string.IsNullOrEmpty(memoryKey))
                {
                    fuzzyKey = CompareKey(key);
                    AddToMemory(key, fuzzyKey);
                }
                else fuzzyKey = memoryKey;
                value = _fuzzyDictionary[fuzzyKey];
            }

            return new KeyValuePair<string, string>(fuzzyKey, value);
        }

        private string CompareKey(string source)
        {
            if (_memoryBuffer.Contains(source))
                return _memoryBuffer[source].ToString();

            double score = 0;
            string resultString = "";
            string resultingValue = "";
            string key = "";
            string[] strS;
            string[] strI;

            string nl = Environment.NewLine;
            string nlnl = nl + nl;

            bool flagNoMatch = false;

            foreach (var item in _fuzzyDictionary)
            {
                flagNoMatch = false;
                key = item.Key;
                if (source.Contains("@"))
                {
                    if (source != item.Key)
                        continue;
                    else
                        return item.Key;
                }

                if (item.Key.IndexOf("@") > 0 && item.Key[0] != source[0])
                    continue;

                if (source.Contains(nl) && (!item.Key.Contains("@newline") && !item.Key.Contains(nl)))
                {
                    continue;
                }
                double tempScore = 0;
                double keyWordsScore = 0;

                if (source.IndexOf(nlnl) != source.LastIndexOf(nlnl) && source.Split(new string[] { nlnl }, StringSplitOptions.None).Length - 1 == 2)
                {
                    return "@key" + nlnl + "@key" + nlnl + "@key";
                }


                strI = key.Split(new string[] { "@key", "@player", "@number", "@playerChild", "@farm" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var substring in strI)
                {
                    if (source.IndexOf(substring) == -1)
                    {
                        flagNoMatch = true;
                        break;
                    }
                }
                if (flagNoMatch)
                    continue;

                strS = source.Replace(nl, " " + nl + " ").Split(' ');
                if (source.Contains(nl))
                    strI = key.Replace(nl, " " + nl + " ").Replace("@key@", "@key @").Replace("@number@", "@number @").Split(' ');
                else
                    strI = key.Replace("@key@", "@key @").Replace("@number@", "@number @").Split(' ');

                if (strS.Length < strI.Length)
                    continue;
                string keyWord = "";
                string keyWordKey = "";
                int prevKeyWordIndex = -1;
                int j = 0;
                double exceptKeyWordsScore = 0;
                int curKeyIndex = 0;
                int curSourceIndex = 0;
                for (int i = 0; i < strS.Length; i++)
                {
                    if (i != 0)
                    {
                        curKeyIndex++;
                        curSourceIndex++;
                        tempScore++;
                    }

                    if (strI[j] == strS[i])
                    {
                        tempScore += strI[j].Length;
                        prevKeyWordIndex = -1;

                        curKeyIndex += strI[j].Length;
                        curSourceIndex += strS[i].Length;
                        j++;
                        if (j >= strI.Length) j = strI.Length - 1;
                        continue;
                    }
                    else
                    {
                        if (strS[i].Length > 0 && j < strI.Length && strI[j].IndexOf("@number") > -1 && !Tools.numberArr.Contains(strS[i][0]))
                        {
                            prevKeyWordIndex = -1;
                            break;
                        }
                    }

                    if (j < strI.Length && !string.IsNullOrEmpty(strI[j]) && strI[j].Contains('@'))
                    {

                        keyWordKey = strI[j];
                        keyWord = strS[i];

                        if (keyWordKey[0] != '@' && keyWordKey[0] != keyWord[0])
                        {
                            prevKeyWordIndex = -1;
                            break;
                        }


                        var tmp = new string[0];
                        if (keyWordKey.Contains("@key") && keyWordKey != "@key")
                            tmp = keyWordKey.Split(new string[] { "@key" }, StringSplitOptions.None);

                        if (tmp.Length == 0 && keyWordKey.Contains("@number") && keyWordKey != "@number")
                            tmp = keyWordKey.Split(new string[] { "@number" }, StringSplitOptions.None);

                        if (tmp.Length > 1)
                        {
                            if (tmp.Length == 2 && !string.IsNullOrEmpty(tmp[1]) && string.IsNullOrEmpty(tmp[0]))
                            {
                                if (string.IsNullOrEmpty(tmp[0]) && !string.IsNullOrEmpty(tmp[1]))
                                {
                                    var ind = keyWord.IndexOf(tmp[1]);
                                    if (ind > 0 && keyWord.Length == ind + tmp[1].Length)
                                    {
                                        keyWord = keyWord.Substring(0, keyWord.Length - tmp[1].Length);
                                        tempScore += tmp[1].Length*3;
                                    }
                                    else
                                    {
                                        tempScore -= tmp[1].Length;
                                    }
                                }
                            }
                            if (tmp.Length == 3 && string.IsNullOrEmpty(tmp[1]) && !string.IsNullOrEmpty(tmp[0]) && !string.IsNullOrEmpty(tmp[2]))
                            {
                                Console.WriteLine("hey2");
                            }
                            if (tmp.Length == 2 && string.IsNullOrEmpty(tmp[1]) && !string.IsNullOrEmpty(tmp[0]))
                            {
                                Console.WriteLine("hey3");
                                keyWord = keyWord.Substring(tmp[0].Length);
                            }
                        }

                        keyWordsScore += keyWord.Length;
                        curKeyIndex += strI[j].Length;
                        curSourceIndex += strS[i].Length;

                        prevKeyWordIndex = j;
                        if (j + 1 < strI.Length && i + 1 < strS.Length)
                        {
                            if (strI[j + 1] == strS[i + 1] || strI[j + 1].Contains("@key"))
                            {
                                if (keyWordKey.Contains("@") && keyWord == "" && (strI[j+1].Contains(nl) || strI[j+1].Contains("@")))
                                    break;
                                j++;
                            }
                            else if (strI[j].Contains("@key") && strI[j + 1].Contains("@key"))
                            {
                                //TODO Check this. if we have this word in _mainDictionary, and don't have this + next word in dictionary, then this key is over
                                if (_simpleDictionary.ContainsKey(keyWord) && !_simpleDictionary.ContainsKey(keyWord + " " + strS[i + 1]))
                                {
                                    j++;
                                }
                            }
                        }

                        if (j >= strI.Length) j = strI.Length - 1;
                        continue;
                    }
                    else if (j < strI.Length && strI[j] == strS[i])
                    {
                        tempScore += strI[j].Length;
                        prevKeyWordIndex = -1;

                        curKeyIndex += strI[j].Length;
                        curSourceIndex += strS[i].Length;
                        j++;
                        if (j >= strI.Length) j = strI.Length - 1;
                    }
                    else if (prevKeyWordIndex > -1)
                    {
                        exceptKeyWordsScore += strS[i].Length;
                        keyWordsScore += strS[i].Length + 1;
                        keyWord += " " + strS[i];

                        // If word after key do not match AND this was a last @key,
                        // we take last part of item.Key and if it is not substing of source, then 
                        // this is not the droids we are looking for
                        if (curKeyIndex < item.Key.Length && item.Key.LastIndexOf("@") < curKeyIndex && source.IndexOf(item.Key.Substring(curKeyIndex)) == -1)
                        {
                            prevKeyWordIndex = -1;
                            break;
                        }

                        curKeyIndex += strI[j].Length;
                        curSourceIndex += strS[i].Length;
                        j++;
                        if (j >= strI.Length) j = strI.Length - 1;
                    }
                    else
                    {
                        prevKeyWordIndex = -1;
                        break;
                    }
                }
                tempScore = tempScore / (source.Length - keyWordsScore) + tempScore / 100;
                if (tempScore >= 0.70 && tempScore > score)
                {
                    score = tempScore;
                    resultString = item.Key;
                    resultingValue = item.Value;
                }
            }

            return resultString;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _simpleDictionary.Concat(_fuzzyDictionary).ToDictionary(i => i.Key, i => i.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _simpleDictionary.Concat(_fuzzyDictionary).ToDictionary(i => i.Key, i => i.Value).GetEnumerator();
        }

        void AddToMemory(string key, string value)
        {
            if (!_memoryBuffer.Contains(key))
                _memoryBuffer.Add(key, value);
            if (_memoryBuffer.Count > 500)
            {
                _memoryBuffer.RemoveAt(0);
            }
        }

        string CheckInMemory(string key)
        {
            if (_memoryBuffer.Contains(key))
                return _memoryBuffer[key].ToString();
            else return string.Empty;
        }
    }

    public static class Tools
    {
        public static Regex reNumber = new Regex("[\\d +-.,:/]+(am|pm)*", RegexOptions.Compiled);
        public static char[] numberArr = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ' ', '+', '-', '.', ',', ':' };
        public static Random rand = new Random();
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        public static void UpdateKey<TKey, TValue>(this IDictionary<TKey, TValue> dic,
                                      TKey fromKey, TKey toKey)
        {
            TValue value = dic[fromKey];
            dic.Remove(fromKey);
            dic[toKey] = value;
        }
        public static void UpdateKeyValue<TKey, TValue>(this IDictionary<TKey, TValue> dic,
                                      TKey fromKey, TKey toKey, TValue value)
        {
            dic.Remove(fromKey);
            dic[toKey] = value;
        }

        public static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        public static void SetInstanceField(Type type, object instance, string fieldName, object value)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            field.SetValue(instance, value);
        }        
    }

    public class Config
    {
        private string _languageName;
        public string LanguageName
        {
            get { return _languageName; }
            set
            {
                _languageName = value;
                UpdateConfigFile();
            }
        }
        private string _executingAssembly;
        public string ExecutingAssembly
        {
            get { return _executingAssembly; }
            set
            {
                _executingAssembly = value;
                UpdateConfigFile();
            }
        }

        private void UpdateConfigFile()
        {
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "languages", "Config.json"),
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this, Formatting.Indented)));
        }
    }
}
