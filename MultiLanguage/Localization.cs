using Cyriller;
using Cyriller.Model;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MultiLanguage
{
    public class Localization
    {
        public Config Config { get; private set; }
        public Dictionary<string, string> Characters { get; set; }

        private JObject _dataRandName { get; set; }
        private Dictionary<string, int> _languages;
        private Dictionary<string, string> _languageDescriptions;
        private FuzzyStringDictionary _fuzzyDictionary;
        private string _currentLanguage;
        private bool _isMenuDrawing;
        private Regex reToSkip = new Regex("^[0-9: -=.g]+$", RegexOptions.Compiled);
        private int IsTranslated;
        private OrderedDictionary _memoryBuffer;
        private List<string> _translatedStrings;
        private int _characterPosition;
        private bool _isGameLoaded = false;
        private bool _isKeyReplaced = false;
        private int _currentUpdate = 0;
        private int _updatesBeforeReplace = 60;
        private string currentName;
        private string PathOnDisk;
        private Assembly _gameAssembly;
        private dynamic _player;
        private dynamic Player
        {
            get
            {
                if(_player == null)
                {
                    var game1Type = _gameAssembly.GetType("StardewValley.Game1");
                    _player = game1Type.GetField("player", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                }
                return _player;
            }
        }
        private bool _isTextInput;
        private List<string> _inputsStrings;

        #region Cyrillic
        private CyrPhrase cyrPhrase;
        private CyrNumber cyrNumber;
        private CyrNounCollection nounCollection;
        private CyrAdjectiveCollection adjectiveCollection;
        #endregion

        public Localization()
        {
            PathOnDisk = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var configLocation = Path.Combine(PathOnDisk, "languages", "Config.json");
            if (!File.Exists(configLocation))
            {
                Config = new Config();
                Config.LanguageName = "EN";
                File.WriteAllBytes(configLocation, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Config)));
            }
            else
            {
                Config = JsonConvert.DeserializeObject<Config>(Encoding.UTF8.GetString(File.ReadAllBytes(configLocation)));
            }
            LoadDictionary();
        }

        public void OnWindowsSizeChanged()
        {
            if (_isMenuDrawing) _isMenuDrawing = false;
        }

        public void OnUpdate(dynamic game)
        {
            #region load game assembly
            if (_gameAssembly == null)
            {
                _gameAssembly = Assembly.LoadFile(Path.Combine(PathOnDisk, Config.ExecutingAssembly));
            }
            var gameType = _gameAssembly.GetType("StardewValley.Game1");
            var player = Tools.GetInstanceField(gameType, game, "player");
            var activeClickableMenu = Tools.GetInstanceField(gameType, game, "activeClickableMenu");
            var uniqueIDForThisGame = Tools.GetInstanceField(gameType, game, "uniqueIDForThisGame");
            dynamic graphics = Tools.GetInstanceField(gameType, game, "graphics");
            #endregion
            #region create report-package (Shift+Alt+L)
            var keyState = Keyboard.GetState();
            if((keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift)) && 
                (keyState.IsKeyDown(Keys.LeftAlt) || keyState.IsKeyDown(Keys.RightAlt)) 
                && keyState.IsKeyDown(Keys.L))
            {
                if (_isGameLoaded)
                {
                    var reportTime = DateTime.Now;
                    var tempFolder = Directory.CreateDirectory(Path.Combine(PathOnDisk, "temp"));
                    if (!Directory.Exists(Path.Combine(PathOnDisk, "reports")))
                        Directory.CreateDirectory(Path.Combine(PathOnDisk, "reports"));
                    var reportFolder = Path.Combine(PathOnDisk, "reports", player.Name + "_" + reportTime.ToString("dd_MM_yyyy_HH_mm_ss"));
                    if (!Directory.Exists(reportFolder))
                        Directory.CreateDirectory(reportFolder);
                    #region screeshot
                    int width = graphics.IsFullScreen ? graphics.PreferredBackBufferWidth : game.Window.ClientBounds.Width;
                    int height = graphics.IsFullScreen ? graphics.PreferredBackBufferHeight : game.Window.ClientBounds.Height;
                    _gameAssembly.GetType("StardewValley.Game1")
                        .GetMethod("Draw", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(game,
                        new object[] { new GameTime() });                    
                    int[] backBuffer = new int[width * height];
                    graphics.GraphicsDevice.GetBackBufferData(backBuffer);
                    var screenshot = new Texture2D(graphics.GraphicsDevice, width, height);
                    screenshot.SetData(backBuffer);
                    Stream stream = File.OpenWrite(Path.Combine(reportFolder, @"screenshot_" + reportTime.ToString("dd_MM_yyyy_HH_mm_ss") + @".jpg"));
                    screenshot.SaveAsJpeg(stream, width, height);
                    stream.Dispose();
                    screenshot.Dispose();
                    #endregion
                    #region copy save files
                    var save = player.Name + "_" + uniqueIDForThisGame.ToString();
                    var savesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "Saves"); 
                    foreach(var fileName in Directory.GetFiles(Path.Combine(savesFolder, save)))
                    {
                        File.Copy(fileName, Path.Combine(tempFolder.FullName, Path.GetFileName(fileName)));
                    }
                    #endregion
                    #region report info
                    dynamic location = Tools.GetInstanceField(gameType, game, "currentLocation");
                    dynamic year = Tools.GetInstanceField(gameType, game, "year");
                    dynamic season = Tools.GetInstanceField(gameType, game, "currentSeason");
                    dynamic day = Tools.GetInstanceField(gameType, game, "dayOfMonth");
                    dynamic time = Tools.GetInstanceField(gameType, game, "timeOfDay");
                    File.WriteAllBytes(Path.Combine(tempFolder.FullName, "reportInfo.json"),
                                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                                    {
                                        Location = location.Name,
                                        Year = year,
                                        Season = season,
                                        Day = day,
                                        Time = time
                                    })));
                    #endregion
                    #region zip
                    FastZip fastZip = new FastZip();
                    fastZip.CreateZip(Path.Combine(reportFolder, "report.zip"),
                        tempFolder.FullName, true, null);
                    //_gameAssembly.GetType("StardewValley.Game1")
                    //    .GetMethod("showGlobalMessage", BindingFlags.Static | BindingFlags.Public)
                    //    .Invoke(game,
                    //    new object[] { string.Format("Report file {0} is created", save + "_" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + ".zip") });
                    #endregion
                    tempFolder.Delete(true);
                }
            }
#if DEBUG
            else if(keyState.IsKeyDown(Keys.LeftShift) && keyState.IsKeyDown(Keys.P))
            {
                dynamic time = Tools.GetInstanceField(gameType, game, "timeOfDay");
                Tools.SetInstanceField(gameType, game, "timeOfDay", time + 100);
                //game
            }
#endif
            #endregion
            #region set new player's name
            if (player != null && !string.IsNullOrEmpty(player.Name) && currentName != player.Name)
            {
                if (_isGameLoaded && !_isKeyReplaced)
                {
                    if (_currentUpdate > _updatesBeforeReplace)
                    {
                        currentName = player.Name;
                        KeyReplace(player.Name, player.farmName);
                    } else
                    {
                        _currentUpdate++;
                    }
                }
            }
            #endregion
            #region check input text
            if (activeClickableMenu != null)
            {
                if (activeClickableMenu.GetType().ToString() == "StardewValley.Menus.TitleMenu")
                {
                    var titleType = _gameAssembly.GetType("StardewValley.Menus.TitleMenu");
                    var subMenu = Tools.GetInstanceField(titleType, activeClickableMenu, "subMenu");
                    if (subMenu != null && subMenu.GetType().ToString() == "StardewValley.Menus.CharacterCustomization")
                    {
                        if (!_isTextInput)
                            _isTextInput = true;
                        var subMenuType = _gameAssembly.GetType("StardewValley.Menus.CharacterCustomization");
                        dynamic nameBox = Tools.GetInstanceField(subMenuType, subMenu, "nameBox");
                        dynamic farmnameBox = Tools.GetInstanceField(subMenuType, subMenu, "farmnameBox");
                        dynamic favThingBox = Tools.GetInstanceField(subMenuType, subMenu, "favThingBox");
                        if (nameBox != null && !string.IsNullOrEmpty(nameBox.Text.ToString()))
                        {
                            if (!_inputsStrings.Contains(nameBox.Text))
                                _inputsStrings.Add(nameBox.Text);
                        }
                        if (farmnameBox != null && !string.IsNullOrEmpty(farmnameBox.Text.ToString()))
                        {
                            if (!_inputsStrings.Contains(farmnameBox.Text))
                                _inputsStrings.Add(farmnameBox.Text);
                        }
                        if (favThingBox != null && !string.IsNullOrEmpty(favThingBox.Text.ToString()))
                        {
                            if (!_inputsStrings.Contains(favThingBox.Text))
                                _inputsStrings.Add(favThingBox.Text);
                        }
                    }
                    else if (_isTextInput)
                    {
                        _isTextInput = false;
                        _inputsStrings.Clear();
                    }
                }
                else if (activeClickableMenu.GetType().ToString() == "StardewValley.Menus.NamingMenu")
                {
                    if (!_isTextInput)
                        _isTextInput = true;
                    var namingMenuType = _gameAssembly.GetType("StardewValley.Menus.NamingMenu");
                    dynamic textBox = Tools.GetInstanceField(namingMenuType, activeClickableMenu, "textBox");
                    if (textBox != null && !string.IsNullOrEmpty(textBox.Text.ToString()))
                    {
                        if (!_inputsStrings.Contains(textBox.Text))
                            _inputsStrings.Add(textBox.Text);
                    }
                }
                else if (_isTextInput)
                {
                    _isTextInput = false;
                    _inputsStrings.Clear();
                }
            }
            #endregion
            #region add language option in Game Menu
            if (activeClickableMenu != null && activeClickableMenu.GetType().ToString() == "StardewValley.Menus.GameMenu")
            {
                var gameMenuType = _gameAssembly.GetType("StardewValley.Menus.GameMenu");
                var optionElementType = _gameAssembly.GetType("StardewValley.Menus.OptionsElement");
                var optionPageType = _gameAssembly.GetType("StardewValley.Menus.OptionsPage");
                var optionsDropDownType = _gameAssembly.GetType("StardewValley.Menus.OptionsDropDown");
                var pages = (Tools.GetInstanceField(gameMenuType, activeClickableMenu, "pages") as IList).Cast<object>();
                
                var optionPage = pages.FirstOrDefault(p => optionPageType == p.GetType());
                if (optionPage != null)
                {
                    var options = Tools.GetInstanceField(optionPageType, optionPage, "options") as IList;
                    if (!_isMenuDrawing)
                    {
                        var listType = typeof(List<>);
                        var constructedListType = listType.MakeGenericType(optionElementType);
                        var newOptions = Activator.CreateInstance(constructedListType) as IList;
                        foreach (var option in options.Cast<dynamic>())
                        {
                            if (option.label == "Sound:")
                            {
                                dynamic languageDropDown = Activator.CreateInstance(optionsDropDownType, "Language", 55, -1, -1);
                                languageDropDown.selectedOption = _languages[_currentLanguage];
                                newOptions.Add(languageDropDown);
                            }
                            newOptions.Add(option);
                        }
                        Tools.SetInstanceField(optionPageType, optionPage, "options", newOptions);
                        _isMenuDrawing = true;
                    }
                }
            }
            #endregion
            else if (activeClickableMenu == null)
            {
                if (_isMenuDrawing)
                    _isMenuDrawing = false;
            }
        }

        public void OnChangeLanguage(int which, int selection, List<string> option)
        {
            if (which == 55)
            {
                var selectedLang = _languageDescriptions[option[selection]];
                if (selectedLang != _currentLanguage)
                {
                    Config.LanguageName = selectedLang;
                    _currentLanguage = selectedLang;
                    var spriteTextType = _gameAssembly.GetType("StardewValley.Game1");
                    var methodInfo = spriteTextType.GetMethod("showGlobalMessage", BindingFlags.Public | BindingFlags.Static);
                    methodInfo.Invoke(null, new object[] { "This change will not take effect until you restart the game" });
                    new Thread(() => LoadContent(false)).Start();
                }
            }
        }

        public void OnSetDropDownPropertyValue(dynamic dropDown)
        {
            if (dropDown.whichOption == 55)
            {
                dropDown.dropDownOptions = _languageDescriptions.Keys.ToList();
            }
        }

        public void OnGameLoaded()
        {
            _isGameLoaded = true;
        }        

        public string OnParseText(string text, SpriteFont whichFont, int width)
        {
            if (Config.LanguageName != "EN")
            {
                var _text = text;
                if (Environment.NewLine != "\n" && text.Contains("\n") && !text.Contains(Environment.NewLine))
                    _text = text.Replace("\n", Environment.NewLine);
                var translateMessage = Translate(_text);
                if (!string.IsNullOrEmpty(translateMessage))
                {
                    _text = translateMessage;
                }

                if (_text == null)
                {
                    return string.Empty;
                }
                string str1 = string.Empty;
                string str2 = string.Empty;
                string str3 = _text;
                foreach (string str4 in str3.Split(' '))
                {
                    if (whichFont.MeasureString(str1 + str4).Length() > width ||
                        str4.Equals(Environment.NewLine))
                    {
                        str2 = str2 + str1 + Environment.NewLine;
                        str1 = string.Empty;
                    }
                    str1 = str1 + str4 + " ";
                }
                return str2 + str1;
            }
            else return string.Empty;
        }

        public DialogueQuestion OnDrawObjectDialogue(string dialogue)
        {
            var result = new DialogueQuestion();
            if (Config.LanguageName != "EN")
            {
                if (_translatedStrings.Contains(dialogue))
                    result.Dialogue = dialogue;
                var translateMessage = Translate(dialogue);
                result.Dialogue = translateMessage;
            }
            else result.Dialogue = dialogue;
            return result;
        }

        public DialogueQuestion OnDrawObjectQuestionDialogue(string dialogue, List<string> choices = null)
        {
            var result = new DialogueQuestion();
            if (Config.LanguageName != "EN")
            {
                if (_translatedStrings.Contains(dialogue))
                {
                    result.Dialogue = dialogue;
                }
                var translateDialogue = Translate(dialogue);
                result.Dialogue = translateDialogue;
                result.Choices = new List<string>();
                if(choices != null)
                {
                    foreach (var chois in choices)
                    {
                        if (_translatedStrings.Contains(chois))
                        {
                            result.Choices.Add(chois);
                        }
                        var translateChois = Translate(chois);
                        result.Choices.Add(chois);
                    }
                }
            }
            else
            {
                result.Dialogue = dialogue;
                result.Choices = choices;
            }
            return result;
        }

        public void OnDrawStringSpriteText(SpriteTextDrawStringEvent @event)
        {
            if (Config.LanguageName != "EN")
            {
                if (_translatedStrings.Contains(@event.Text))
                    return;
                var originalText = @event.Text;
                var translateText = @event.Text;
                if (Characters.ContainsKey(@event.Text))
                {
                    translateText = Characters[@event.Text];
                    if (!_translatedStrings.Contains(translateText)) _translatedStrings.Add(translateText);
                }
                else
                {
                    var translateMessage = Translate(@event.Text);
                    if (!string.IsNullOrEmpty(translateMessage))
                    {
                        translateText = translateMessage;
                    }
                }
                if (originalText.Length > @event.CharacterPosition || @event.CharacterPosition == 999999)
                {
                    _characterPosition = @event.CharacterPosition;
                }
                else if (_characterPosition < translateText.Length)
                {
                    _characterPosition++;
                }
                drawString(@event.Sprite, translateText, @event.X, @event.Y, _characterPosition,
                    @event.Width, @event.Height, @event.Alpha, @event.LayerDepth, @event.JunimoText,
                    @event.DrawBGScroll, @event.PlaceHolderScrollWidthText, @event.Color);
                @event.ReturnEarly = true;
            }
        }

        public int OnGetWidthSpriteText(string text)
        {
            if (Config.LanguageName != "EN")
            {
                if (IsTranslated > 0)
                {
                    IsTranslated = 0;
                    return -1;
                }
                var translateMessage = Translate(text, false);

                if (!string.IsNullOrEmpty(translateMessage))
                {
                    IsTranslated++;
                    var spriteTextType = _gameAssembly.GetType("StardewValley.BellsAndWhistles.SpriteText");
                    var methodInfo = spriteTextType.GetMethod("getWidthOfString", BindingFlags.Public | BindingFlags.Static);
                    var result = Convert.ToInt32(methodInfo.Invoke(null, new object[] { translateMessage }));
                    return result;
                }
                else if (Characters.ContainsKey(text))
                {
                    IsTranslated++;
                    var spriteTextType = _gameAssembly.GetType("StardewValley.BellsAndWhistles.SpriteText");
                    var methodInfo = spriteTextType.GetMethod("getWidthOfString", BindingFlags.Public | BindingFlags.Static);
                    var result = Convert.ToInt32(methodInfo.Invoke(null, new object[] { Characters[text] }));
                    return result;
                }
                else return -1;
            }
            else return -1;
        }

        public int OnGetHeightSpriteText(string text, int widthConstraint)
        {
            if (Config.LanguageName != "EN")
            {
                if (IsTranslated > 0)
                {
                    IsTranslated = 0;
                    return -1;
                }
                var translateMessage = Translate(text, false);

                if (!string.IsNullOrEmpty(translateMessage))
                {
                    IsTranslated++;
                    var spriteTextType = _gameAssembly.GetType("StardewValley.BellsAndWhistles.SpriteText");
                    var methodInfo = spriteTextType.GetMethod("getHeightOfString", BindingFlags.Public | BindingFlags.Static);
                    var result = Convert.ToInt32(methodInfo.Invoke(null, new object[] { translateMessage, widthConstraint }));
                    return result;
                }
                else if (Characters.ContainsKey(text))
                {
                    IsTranslated++;
                    var spriteTextType = _gameAssembly.GetType("StardewValley.BellsAndWhistles.SpriteText");
                    var methodInfo = spriteTextType.GetMethod("getHeightOfString", BindingFlags.Public | BindingFlags.Static);
                    var result = Convert.ToInt32(methodInfo.Invoke(null, new object[] { Characters[text], widthConstraint }));
                    return result;
                }
                else return -1;
            }
            else return -1;
        }

        public string OnSpriteBatchDrawString(string message)
        {
            if (Config.LanguageName != "EN")
            {
                if (_translatedStrings.Contains(message))
                    return message;
                var translateMessage = Translate(message);
                return translateMessage;
            }
            else return string.Empty;
        }

        public string OnSpriteFontMeasureString(string message)
        {
            if (Config.LanguageName != "EN")
            {
                if (reToSkip.IsMatch(message) || string.IsNullOrEmpty(message))
                    return string.Empty;
                if (_translatedStrings.Contains(message))
                    return message;
                var translateMessage = Translate(message);
                return translateMessage;
            }
            else return string.Empty;
        }

        public List<string> OnStringBrokeIntoSections(string letter, int width, int height)
        {
            if (Config.LanguageName != "EN")
            {
                if (!_isKeyReplaced)
                {
                    KeyReplace(Player.Name, Player.farmName);
                }
                var translateMessage = Translate(letter);

                if (!string.IsNullOrEmpty(translateMessage))
                {
                    List<string> list = new List<string>();
                    var s = translateMessage;
                    for (; s.Length > 0; s = s.Substring(list.Last().Length))
                    {
                        var spriteTextType = _gameAssembly.GetType("StardewValley.BellsAndWhistles.SpriteText");
                        var methodInfo = spriteTextType.GetMethod("getStringPreviousToThisHeightCutoff", BindingFlags.Public | BindingFlags.Static);
                        string thisHeightCutoff = methodInfo.Invoke(null, new object[] { s, width, height }).ToString();
                        if (thisHeightCutoff.Length > 0)
                            list.Add(thisHeightCutoff);
                        else
                            break;
                    }
                    return list;
                }
                else return null;
            }
            else return null;
        }

        public string OnSparklingTextCallback(string text)
        {
            if (Config.LanguageName != "EN")
            {
                var translateMessage = Translate(text);
                if (!string.IsNullOrEmpty(translateMessage))
                {
                    return translateMessage;
                }
            }
            return string.Empty;
        }

        private string Translate(string message, bool needTrim = true)
        {
            if (Config.LanguageName != "EN")
            {
                if (needTrim)
                    message = message.Trim();

                if (string.IsNullOrEmpty(message) || 
                    reToSkip.IsMatch(message) || 
                    _inputsStrings.Contains(message) ||
                    _translatedStrings.Contains(message.Replace("\r\n", "").Replace("\n", "")))
                {
                    return message;
                }
                if (_memoryBuffer.Contains(message))
                {
                    if (!_memoryBuffer[message].ToString().IsNullOrEmpty())
                        return _memoryBuffer[message].ToString();
                    else
                        return message;
                }
                if (_fuzzyDictionary.ContainsKey(message))
                {
                    try
                    {
                        var resultTranslate = message;
                        var fval = _fuzzyDictionary.GetFuzzyKeyValue(message);

                       var tempFKey = fval.Key;
                        var tempFValue = fval.Value;

                        if (tempFValue.Contains("^") && !tempFKey.Contains("^"))
                        {
                            if (Player.IsMale)
                            {
                                tempFValue = tempFValue.Split('^')[0];
                            }
                            else tempFValue = tempFValue.Split('^')[1];
                        }

                        if (tempFValue.Contains("/") &&
                            !tempFValue.Contains("@number/") &&
                            !tempFValue.Contains("Бег/Ходьба") &&
                            !tempFValue.Contains("Проверить/Выполнить") &&
                            !tempFValue.Contains("24/7") &&
                            !tempFValue.Contains("http"))
                        {
                            var genderSplit = tempFValue.Split(' ', ',', '.', '!', '?', '<', '=', '-', ':', '^')
                                .Where(s => s.Contains('/'))
                                .Select(s => new KeyValuePair<string, string>(s, Player.IsMale ? s.Split('/')[0] : s.Split('/')[1]));
                            foreach (var gend in genderSplit)
                            {
                                tempFValue = tempFValue.Replace(gend.Key, gend.Value);
                            }
                        }
                        if (tempFValue.Contains("|"))
                        {
                            dynamic npc = _gameAssembly.GetType("StardewValley.Game1").GetField("currentSpeaker", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                            if (npc == null && string.IsNullOrEmpty(Player.spouse))
                            {
                                var spriteTextType = _gameAssembly.GetType("StardewValley.Game1");
                                var methodInfo = spriteTextType.GetMethod("getCharacterFromName", BindingFlags.Public | BindingFlags.Static);
                                npc = methodInfo.Invoke(null, new object[] { Player.spouse });
                            }
                            if (npc != null)
                            {
                                var genderSplit = tempFValue.Split(' ', ',', '.', '!', '?', '<', '=', '-', ':', '^')
                                    .Where(s => s.Contains('|'))
                                    .Select(s => new KeyValuePair<string, string>(s, npc.gender == 1 ? s.Split('|')[1] : s.Split('|')[0]));
                                foreach (var gend in genderSplit)
                                {
                                    tempFValue = tempFValue.Replace(gend.Key, gend.Value);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(tempFKey) && !string.IsNullOrEmpty(tempFValue))
                        {
                            if (tempFKey.Contains("@"))
                            {
                                var diff = GetKeysValue(tempFKey, message);
                                resultTranslate = StringFormatWithKeys(tempFValue, diff.Select(d => d.Value).ToList());
                            }
                            else
                            {
                                resultTranslate = tempFValue;
                            }
                        }

                        if (_memoryBuffer.Count > 500)
                        {
                            _memoryBuffer.RemoveAt(0);
                        }
                        if (!_memoryBuffer.Contains(message))
                        {
                            _memoryBuffer.Add(message, resultTranslate);
                        }
                        if (!_translatedStrings.Contains(resultTranslate))
                        {
                            _translatedStrings.Add(resultTranslate);
                            if (_translatedStrings.Count > 500)
                                _translatedStrings.RemoveAt(0);
                        }
                        return resultTranslate;
                    }
                    catch
                    {
                        return message;
                    }                    
                }
                else
                {
                    if (!_memoryBuffer.Contains(message))
                    {
                        _memoryBuffer.Add(message, string.Empty);
                        if (_memoryBuffer.Count > 500)
                        {
                            _memoryBuffer.RemoveAt(0);
                        }
                    }
                    return message;
                }
            }
            return "";
        }

        private List<KeyValuePair<string, string>> GetKeysValue(string template, string str)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            string pattern = Regex.Escape(template);
            MatchCollection matches;
            int count = 0;
            while ((matches = Regex.Matches(pattern, "@key|@number|@farm|@player|@playerChild")).Count != 0)
            {
                pattern = pattern.Remove(matches[0].Index, matches[0].Length);
                pattern = pattern.Insert(matches[0].Index, "(.+?)");
                result.Add(new KeyValuePair<string, string>(matches[0].Value, ""));
                count++;
            }

            pattern += "$";
        TryAgain:
            Regex r = new Regex(pattern, RegexOptions.Singleline);
            Match m = r.Match(str);
            for (int i = 1; i < m.Groups.Count; i++)
            {
                var key = result[i - 1].Key;
                result[i - 1] = new KeyValuePair<string, string>(key, m.Groups[i].Value);
                //if we have (.+?)\ (.+?) in pattern, on second (.+?) we need to this check:
                if (pattern.IndexOf(@"(.+?)\ (.+?)") > -1 && !reToSkip.IsMatch(m.Groups[i].Value) && !_fuzzyDictionary.ContainsKey(m.Groups[i].Value)) {
                    // change first "(.+?)" to "(.+?\ .+?)" and start from begining of cycle
                    pattern = pattern.ReplaceFirst("(.+?)", "(.+? .+?)");
                    goto TryAgain;
                }
            }
            return result;
        }

        private string StringFormatWithKeys(string format, List<string> args)
        {
            string result = format;
            MatchCollection matches;
            int i = 0;
            var originalMatches = Regex.Matches(format, "@keyS[RDVTP]{0,1}|@key[RDVTP]{0,1}|@number|@farm|@player|@playerChild");
            decimal lanstNumber = -1;
            while ((matches = Regex.Matches(result, "@keyS[RDVTP]{0,1}|@key[RDVTP]{0,1}|@number|@farm|@player|@playerChild")).Count != 0)
            {
                var value = args[i];
                if (matches[0].Value.Contains("@key"))
                {
                    if (i == 2 &&
                        format == "@key" + Environment.NewLine + Environment.NewLine + "@key" + Environment.NewLine + Environment.NewLine + "@key"
                        && value.Contains(Environment.NewLine))
                    {
                        string newValue = "";
                        foreach (var item in value.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                        {
                            newValue += Translate(item) + Environment.NewLine;
                        }
                        value = newValue.Substring(0, newValue.Length - Environment.NewLine.Length);
                    }
                    else {
                        var tmp = Translate(value);
                        if (!string.IsNullOrEmpty(tmp))
                            value = tmp;
                    }

                    if(Config.LanguageName == "RU")
                    {
                        if(matches[0].Value.Length >= 5)
                        {
                            if(matches[0].Value[4] == 'S')
                            {
                                if(lanstNumber != -1)
                                {
                                    if (matches[0].Value.Length == 5)
                                        value = DeclinePlural(value, "I", lanstNumber);
                                    else value = DeclinePlural(value, matches[0].Value.Last().ToString(), lanstNumber);
                                }
                                else
                                {
                                    if (matches[0].Value.Length == 5)
                                        value = DeclinePlural(value, "I");
                                    else value = DeclinePlural(value, matches[0].Value.Last().ToString());
                                }
                            }
                            else value = Decline(value, matches[0].Value.Last().ToString());
                        }
                    }
                }
                else if(matches[0].Value == "@number" && value != "" && Regex.Match(value, @"\d+").Length > 0)
                {
                    try
                    {
                        lanstNumber = Convert.ToDecimal(value);
                    }
                    catch
                    {
                        lanstNumber = Convert.ToDecimal(Regex.Match(value, @"\d+").Value);
                    }
                }
                result = result.Remove(matches[0].Index, matches[0].Length);
                result = result.Insert(matches[0].Index, value);
                i++;
            }
            return result;
        }

        private string Decline(string message, string _case)
        {
            try
            {
                var result = cyrPhrase.Decline(message, GetConditionsEnum.Similar);
                string res = "";
                switch (_case)
                {
                    case "R":
                        res = result.Genitive;
                        break;
                    case "D":
                        res = result.Dative;
                        break;
                    case "V":
                        res = result.Accusative;
                        break;
                    case "T":
                        res = result.Instrumental;
                        break;
                    case "P":
                        res = result.Prepositional;
                        break;
                    case "N":
                    default:
                        res = result.Nominative;
                        break;
                }
                return res;
            }
            catch (Exception)
            {
                Console.WriteLine("Decline Exception, try to add a word " + message);
                return message;
                throw;
            }
        }
        private string DeclinePlural(string message, string _case)
        {
            try
            {
                var result = new CyrResult();
                CyrNoun noun;
                CyrAdjective adj;
                if (message.Contains(" "))
                {
                    noun = nounCollection.Get(message.Split(' ').Last(), GetConditionsEnum.Similar);
                    try
                    {
                        adj = adjectiveCollection.Get(message.Split(' ').First(), GetConditionsEnum.Similar, noun.Gender);
                        result.Add(adj.DeclinePlural(noun.Animate));
                        result.Add(noun.DeclinePlural());
                    }
                    catch
                    {
                        result = cyrPhrase.DeclinePlural(message, GetConditionsEnum.Similar);
                    }
                }
                else
                {
                    noun = nounCollection.Get(message, GetConditionsEnum.Similar);
                    result.Add(noun.DeclinePlural());
                }
                string res = "";
                switch (_case)
                {
                    case "R":
                        res = result.Genitive.Replace("-", " ");
                        break;
                    case "D":
                        res = result.Dative.Replace("-", " ");
                        break;
                    case "V":
                        res = result.Accusative.Replace("-", " ");
                        break;
                    case "T":
                        res = result.Instrumental.Replace("-", " ");
                        break;
                    case "P":
                        res = result.Prepositional.Replace("-", " ");
                        break;
                    case "N":
                    default:
                        res = result.Nominative.Replace("-", " ");
                        break;
                }
                return res;
            }
            catch (Exception)
            {
                Console.WriteLine("Decline Exception, try to add a word " + message);
                return message;
                throw;
            }
        }
        private string DeclinePlural(string message, string _case, decimal number)
        {
            try
            {
                CyrNumber cyr = new CyrNumber();
                CyrNoun noun;
                CyrNumber.Item item;
                if (message.Contains(" "))
                {
                    noun = nounCollection.Get(message.Split(' ').Last(), GetConditionsEnum.Similar);
                    item = new CyrNumber.Item(noun, message.Split(' ')[0]);
                }
                else
                {
                    noun = nounCollection.Get(message, GetConditionsEnum.Similar);
                    item = new CyrNumber.Item(noun);
                }
                var result = cyr.Decline(number, item);
                string res = "";
                switch (_case)
                {
                    case "R":
                        res = string.Join(" ", result.Genitive.Split(' ').SubArray(1, result.Genitive.Split(' ').Length - 1));
                        break;
                    case "D":
                        res = string.Join(" ", result.Dative.Split(' ').SubArray(1, result.Genitive.Split(' ').Length - 1));
                        break;
                    case "V":
                        res = string.Join(" ", result.Accusative.Split(' ').SubArray(1, result.Genitive.Split(' ').Length - 1));
                        break;
                    case "T":
                        res = string.Join(" ", result.Instrumental.Split(' ').SubArray(1, result.Genitive.Split(' ').Length - 1));
                        break;
                    case "P":
                        res = string.Join(" ", result.Prepositional.Split(' ').SubArray(1, result.Genitive.Split(' ').Length - 1));
                        break;
                    case "N":
                    default:
                        res = string.Join(" ", result.Nominative.Split(' ').SubArray(1, result.Genitive.Split(' ').Length - 1));
                        break;
                }
                return res;
            }
            catch (Exception)
            {
                Console.WriteLine("Decline Exception, try to add a word " + message);
                return message;
                throw;
            }
        }

        private void AddToDictionary(string key, string value)
        {
            if (key != "__comment")
            {
                _fuzzyDictionary.Add(key.Trim(), value);
            }
        }

        private void KeyReplace(string playerName, string farm)
        {
            // we need to cache the keys to update since we can't
            // modify the collection during enumeration
            var keysToUpdate = new List<string>();
            foreach (var row in _fuzzyDictionary)
            {
                if (row.Key.Contains("@player") || row.Key.Contains("@farm"))// || row.Key.Contains("%farm") || row.Key.Contains("@"))
                {
                    keysToUpdate.Add(row.Key);
                }
            }
            foreach (var keyToUpdate in keysToUpdate)
            {
                var value = _fuzzyDictionary[keyToUpdate];

                var newKey = keyToUpdate.Replace("@player", playerName).Replace("@farm", farm);//.Replace("%farm", farm).Replace("@", playerName);
                var newValue = value.Replace("@player", playerName).Replace("@farm", farm);//.Replace("%farm", farm).Replace("@", playerName);
                
                _fuzzyDictionary.Remove(keyToUpdate);
                _fuzzyDictionary.Add(newKey, newValue);
            }

            _isKeyReplaced = true;
        }
        
        private void drawString(SpriteBatch b, string s, int x, int y, int characterPosition,
                                int width, int height, float alpha, float layerDepth, bool junimoText,
                                int drawBGScroll, string placeHolderScrollWidthText, int color)
        {
            var spriteTextType = _gameAssembly.GetType("StardewValley.BellsAndWhistles.SpriteText");
            var game1Type = _gameAssembly.GetType("StardewValley.Game1");
            int fontPixelZoom = (int)spriteTextType.GetField("fontPixelZoom", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            int pixelZoom = (int)game1Type.GetField("pixelZoom", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            dynamic graphics = game1Type.GetField("graphics", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            Texture2D mouseCursors = game1Type.GetField("mouseCursors", BindingFlags.Static | BindingFlags.Public).GetValue(null) as Texture2D;
            dynamic coloredTexture = spriteTextType.GetField("coloredTexture", BindingFlags.Static | BindingFlags.Public).GetValue(null) as Texture2D;
            dynamic spriteTexture = spriteTextType.GetField("spriteTexture", BindingFlags.Static | BindingFlags.Public).GetValue(null) as Texture2D;

            var getWidthOfStringInfo = spriteTextType.GetMethod("getWidthOfString", BindingFlags.Public | BindingFlags.Static);
            var getWidthOffsetForCharInfo = spriteTextType.GetMethod("getWidthOffsetForChar", BindingFlags.Public | BindingFlags.Static);
            var positionOfNextSpaceInfo = spriteTextType.GetMethod("positionOfNextSpace", BindingFlags.Public | BindingFlags.Static);
            var getColorFromIndexInfo = spriteTextType.GetMethod("getColorFromIndex", BindingFlags.Public | BindingFlags.Static);

            if (width == -1)
            {
                width = graphics.GraphicsDevice.Viewport.Width - x;
                if (drawBGScroll == 1)
                {
                    width = Convert.ToInt32(getWidthOfStringInfo.Invoke(null, new object[] { s })) * 2;
                }
            }
            if (fontPixelZoom < 4)
            {
                y = y + (4 - fontPixelZoom) * pixelZoom;
            }
            Vector2 position = new Vector2(x, y);
            int accumulatedHorizontalSpaceBetweenCharacters = 0;
            if (drawBGScroll != 1)
            {
                if (position.X + width > graphics.GraphicsDevice.Viewport.Width - pixelZoom)
                {
                    Viewport viewport = graphics.GraphicsDevice.Viewport;
                    position.X = (float)(viewport.Width - width - pixelZoom);
                }
                if (position.X < 0f)
                {
                    position.X = 0f;
                }
            }
            if (drawBGScroll == 0)
            {
                b.Draw(mouseCursors, position + (new Vector2(-12f, -3f) * (float)pixelZoom),
                    new Rectangle?(new Rectangle(325, 318, 12, 18)), Color.White * alpha, 0f, Vector2.Zero,
                    (float)pixelZoom, SpriteEffects.None, layerDepth - 0.001f);
                b.Draw(mouseCursors, position + (new Vector2(0f, -3f) * (float)pixelZoom),
                    new Rectangle?(new Rectangle(337, 318, 1, 18)), Color.White * alpha, 0f, Vector2.Zero,
                    new Vector2(Convert.ToSingle(getWidthOfStringInfo.Invoke(null, new object[] { (placeHolderScrollWidthText.Count<char>() > 0
                                ? placeHolderScrollWidthText
                                : s) })), (float)pixelZoom),
                    SpriteEffects.None, layerDepth - 0.001f);
                b.Draw(mouseCursors,
                    position + new Vector2(Convert.ToSingle(getWidthOfStringInfo.Invoke(null, new object[] { placeHolderScrollWidthText.Count<char>() > 0 ? placeHolderScrollWidthText : s })),
                                (float)(-3 * pixelZoom)), new Rectangle?(new Rectangle(338, 318, 12, 18)),
                                Color.White * alpha, 0f, Vector2.Zero, (float)pixelZoom, SpriteEffects.None,
                                layerDepth - 0.001f);
                if (placeHolderScrollWidthText.Count<char>() > 0)
                {
                    x = x + ((int)getWidthOfStringInfo.Invoke(null, new object[] { placeHolderScrollWidthText }) / 2 - (int)getWidthOfStringInfo.Invoke(null, new object[] { (s) }) / 2);
                    position.X = x;
                }
                position.Y = position.Y + (4 - fontPixelZoom) * pixelZoom;
            }
            else if (drawBGScroll == 1)
            {
                b.Draw(mouseCursors, position + (new Vector2(-7f, -3f) * (float)pixelZoom),
                    new Rectangle?(new Rectangle(324, 299, 7, 17)), Color.White * alpha, 0f, Vector2.Zero,
                    (float)pixelZoom, SpriteEffects.None, layerDepth - 0.001f);
                b.Draw(mouseCursors, position + (new Vector2(0f, -3f) * (float)pixelZoom),
                    new Rectangle?(new Rectangle(331, 299, 1, 17)), Color.White * alpha, 0f, Vector2.Zero,
                    new Vector2(Convert.ToSingle(getWidthOfStringInfo.Invoke(null, new object[] {(placeHolderScrollWidthText.Count() > 0
                                ? placeHolderScrollWidthText
                                : s)})), pixelZoom), SpriteEffects.None, layerDepth - 0.001f);
                b.Draw(mouseCursors,
                    position +
                    new Vector2(Convert.ToSingle(getWidthOfStringInfo.Invoke(null, new object[] {(placeHolderScrollWidthText.Count() > 0
                                ? placeHolderScrollWidthText
                                : s)})), -3 * pixelZoom), new Rectangle?(new Rectangle(332, 299, 7, 17)),
                    Color.White * alpha, 0f, Vector2.Zero, pixelZoom, SpriteEffects.None,
                    layerDepth - 0.001f);
                b.Draw(mouseCursors, position + new Vector2(Convert.ToSingle(getWidthOfStringInfo.Invoke(null, new object[] {(placeHolderScrollWidthText.Count() > 0
                                                                ? placeHolderScrollWidthText : s)})) / 2, 13 * pixelZoom), new Rectangle?(new Rectangle(341, 308, 6, 5)),
                    Color.White * alpha, 0f, Vector2.Zero, pixelZoom, SpriteEffects.None,
                    layerDepth - 0.0001f);
                if (placeHolderScrollWidthText.Count<char>() > 0)
                {
                    x = x + (int)getWidthOfStringInfo.Invoke(null, new object[] { placeHolderScrollWidthText }) / 2 - (int)getWidthOfStringInfo.Invoke(null, new object[] { s }) / 2;
                    position.X = x;
                }
                position.Y = position.Y + (float)((4 - fontPixelZoom) * pixelZoom);
            }
            else if (drawBGScroll == 2)
            {
                b.Draw(mouseCursors, position + (new Vector2(-3f, -3f) * (float)pixelZoom),
                    new Rectangle?(new Rectangle(327, 281, 3, 17)), Color.White * alpha, 0f, Vector2.Zero,
                    (float)pixelZoom, SpriteEffects.None, layerDepth - 0.001f);
                b.Draw(mouseCursors, position + (new Vector2(0f, -3f) * (float)pixelZoom),
                    new Rectangle?(new Rectangle(330, 281, 1, 17)), Color.White * alpha, 0f, Vector2.Zero,
                    new Vector2(Convert.ToSingle((int)getWidthOfStringInfo.Invoke(null, new object[] {(placeHolderScrollWidthText.Count<char>() > 0
                                ? placeHolderScrollWidthText
                                : s) }) + pixelZoom), (float)pixelZoom), SpriteEffects.None,
                    layerDepth - 0.001f);
                b.Draw(mouseCursors,
                    position +
                    new Vector2(
                        (float)
                            ((int)getWidthOfStringInfo.Invoke(null, new object[] {(placeHolderScrollWidthText.Count<char>() > 0
                                ? placeHolderScrollWidthText
                                : s) }) + pixelZoom), (float)(-3 * pixelZoom)),
                    new Rectangle?(new Rectangle(333, 281, 3, 17)), Color.White * alpha, 0f, Vector2.Zero,
                    (float)pixelZoom, SpriteEffects.None, layerDepth - 0.001f);
                if (placeHolderScrollWidthText.Count<char>() > 0)
                {
                    x = x +
                        ((int)getWidthOfStringInfo.Invoke(null, new object[] { placeHolderScrollWidthText }) / 2 - (int)getWidthOfStringInfo.Invoke(null, new object[] { s }) / 2);
                    position.X = (float)x;
                }
                position.Y = position.Y + (float)((4 - fontPixelZoom) * pixelZoom);
            }
            s = s.Replace(Environment.NewLine, "");
            for (int i = 0; i < Math.Min(s.Length, characterPosition); i++)
            {
                if (s[i] != '\u005E')
                {
                    if (i > 0)
                    {
                        position.X = position.X +
                            (float)(8 * fontPixelZoom + accumulatedHorizontalSpaceBetweenCharacters +
                            (int)getWidthOffsetForCharInfo.Invoke(null, new object[] { s[i] }) +
                            (int)getWidthOffsetForCharInfo.Invoke(null, new object[] { s[i - 1] }) * fontPixelZoom);
                    }
                    int num = fontPixelZoom;
                    accumulatedHorizontalSpaceBetweenCharacters = 0;
                    if ((int)positionOfNextSpaceInfo.Invoke(null, new object[] { s, i, (int)position.X, accumulatedHorizontalSpaceBetweenCharacters }) >= x + width - pixelZoom)
                    {
                        position.Y = position.Y + (float)(18 * fontPixelZoom);
                        accumulatedHorizontalSpaceBetweenCharacters = 0;
                        position.X = (float)x;
                    }
                    b.Draw((color != -1 ? coloredTexture : spriteTexture), position,
                        new Rectangle?(getSourceRectForChar(s[i], junimoText)),
                        (Color)getColorFromIndexInfo.Invoke(null, new object[] { color }) * alpha, 0f, Vector2.Zero, (float)fontPixelZoom,
                        SpriteEffects.None, layerDepth);
                }
                else
                {
                    position.Y = position.Y + (float)(18 * fontPixelZoom);
                    position.X = (float)x;
                    accumulatedHorizontalSpaceBetweenCharacters = 0;
                }
            }
        }

        private Rectangle getSourceRectForChar(char c, bool junimoText)
        {
            var spriteTextType = _gameAssembly.GetType("StardewValley.BellsAndWhistles.SpriteText");
            dynamic spriteTexture = spriteTextType.GetField("spriteTexture", BindingFlags.Static | BindingFlags.Public).GetValue(null) as Texture2D;
            int num = (int)c - 32;
            return new Rectangle(num * 8 % spriteTexture.Width, num * 8 / spriteTexture.Width * 16 + (junimoText ? 96 : 0), 8, 16);
        }

        private void LoadDictionary()
        {
            _isTextInput = false;
            _inputsStrings = new List<string>();
            _memoryBuffer = new OrderedDictionary();
            _translatedStrings = new List<string>();
            _languages = new Dictionary<string, int>();
            _fuzzyDictionary = new FuzzyStringDictionary();
            Characters = new Dictionary<string, string>();
            var jobj = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(PathOnDisk, "languages", "descriptions.json"))));
            _languageDescriptions = new Dictionary<string, string>();
            foreach (var directory in Directory.GetDirectories(Path.Combine(PathOnDisk, "languages")).Select((o, i) => new { Value = o, Index = i }))
            {
                var shortName = directory.Value.Split('\\').Last();
                _languageDescriptions.Add(jobj[shortName].ToString(), shortName);
                _languages.Add(shortName, directory.Index);
            }

            _currentLanguage = Config.LanguageName;
            var dictionariesFolder = Path.Combine(PathOnDisk, "languages", Config.LanguageName, "dictionaries");
            if (Directory.Exists(dictionariesFolder) && Directory.GetFiles(dictionariesFolder).Count() > 0)
            {
                foreach (var dict in Directory.GetFiles(dictionariesFolder))
                {
                    var dictName = Path.GetFileName(dict);
                    if (dictName == "Dialogues.json")
                    {
                        var jo = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                        foreach (var val in jo)
                        {
                            var pair = JObject.Parse(val.Value.ToString());
                            foreach (var row in pair)
                            {
                                AddToDictionary(row.Key, row.Value.ToString());
                            }
                        }
                    }
                    if (dictName == "Items.json")
                    {
                        var jo = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                        foreach (var val in jo)
                        {
                            var pair = JObject.Parse(val.Value.ToString());
                            foreach (var row in pair)
                            {
                                AddToDictionary(row.Key, row.Value.ToString());
                            }
                        }
                    }
                    else if (dictName == "Characters.json")
                    {
                        Characters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                        foreach (var pair in Characters)
                        {
                            AddToDictionary(pair.Key, pair.Value);
                        }
                    }
                    else if (dictName == "Achievements.json" || dictName == "animationDescription.json" || dictName == "EngagementDialogue.json" ||
                        dictName == "Events.json" || dictName == "Festivals.json" ||
                        dictName == "Mails.json" || dictName == "Quests.json" ||
                        dictName == "NPCGiftTastes.json"  || dictName == "schedules.json" ||
                        dictName == "TV.json")
                    {
                        var jo = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                        foreach (var val in jo)
                        {
                            AddToDictionary(val.Key, val.Value.ToString());
                        }
                    }
                    else if (dictName == "_NameGen.json")
                    {
                        _dataRandName = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                    }
                    else
                    {
                        var jo = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)).Replace("@newline", Environment.NewLine));
                        foreach (var pair in jo)
                        {
                            AddToDictionary(pair.Key, pair.Value.ToString());
                        }
                    }
                }
            }

            LoadContent(true);

            if (Config.LanguageName == "RU")
            {
                nounCollection = new CyrNounCollection();
                adjectiveCollection = new CyrAdjectiveCollection();
                cyrPhrase = new CyrPhrase(nounCollection, adjectiveCollection);
                cyrNumber = new CyrNumber();
            }
        }



        public void LoadContent(bool onlyNew)
        {
            var modeContentFolder = Path.Combine(PathOnDisk, "languages", Config.LanguageName, "content");
            var gameContentFolder = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "Content");// Game1.content.RootDirectory);
            foreach (var directory in Directory.GetDirectories(modeContentFolder))
            {
                var files = Directory.GetFiles(directory).Where(f => Path.GetExtension(f) == ".xnb");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var gameFile = new FileInfo(Path.Combine(gameContentFolder, directory.Split('\\').Last(), fileName));
                    var modeFile = new FileInfo(Path.Combine(directory, fileName));
                    if (gameFile.Exists)
                    {
                        if ((onlyNew && gameFile.LastWriteTime != modeFile.LastWriteTime) || !onlyNew)
                        {
                            modeFile.CopyTo(gameFile.FullName, true);
                        }
                    }
                    if (fileName == "townInterior.xnb" ||
                        fileName == "HospitalTiles.xnb" || 
                        fileName == "bathhouse_tiles.xnb" ||
                        fileName == "desertTiles.xnb" ||
                        fileName == "fall_outdoorsTileSheet.xnb" ||
                        fileName == "fall_town.xnb" ||
                        fileName == "Festivals.xnb" ||
                        fileName == "HospitalTiles.xnb" ||
                        fileName == "spring_beach.xnb" ||
                        fileName == "spring_outdoorsTileSheet.xnb" ||
                        fileName == "spring_town.xnb" ||
                        fileName == "spring_townbuildings.xnb" ||
                        fileName == "summer_beach.xnb" ||
                        fileName == "TownIndoors.xnb" ||
                        fileName == "winter_beach.xnb" ||
                        fileName == "winter_outdoorsTileSheet.xnb" ||
                        fileName == "winter_town.xnb")
                    {
                        gameFile = new FileInfo(Path.Combine(gameContentFolder, fileName));
                        if (gameFile.Exists)
                        {
                            if ((onlyNew && gameFile.LastWriteTime != modeFile.LastWriteTime) || !onlyNew)
                            {
                                modeFile.CopyTo(gameFile.FullName, true);
                            }
                        }
                    }
                }
            }
        }
    }
    public class DialogueQuestion
    {
        public string Dialogue { get; set; }
        public List<string> Choices { get; set; }
        public DialogueQuestion()
        {
            Choices = new List<string>();
        }
    }
}
