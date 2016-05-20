using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MultiLanguage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LanguagePatcher
{
    class Program
    {
        static AssemblyDefinition GameAssembly { get; set; }
        static void Main(string[] args)
        {
            LocalizationBridge.Localization = new Localization();
            GameAssembly = AssemblyDefinition.ReadAssembly(LocalizationBridge.Localization.Config.ExecutingAssembly);
            if (!CheckPatchedMark())
            {
                while(true)
                {
                    Console.WriteLine("Version of your game is not localized. Do you want to patch the game? (y = Yes; n = No");
                    var answer = Console.ReadLine();
                    if (answer == "y")
                    {
                        break;
                    }
                    else if(answer == "n")
                    {
                        StartGame("Stardew Valley.exe");
                        return;
                    }
                }
                while (true)
                {
                    Console.WriteLine("Do you want to create new 'Stardew Valley (multilang).exe' file (can't using mods) or rewrite  original 'Stardew Valley.exe'? (1 = Create; 2 = Rewrite");
                    var answer = Console.ReadLine();
                    if (answer == "1")
                    {
                        LocalizationBridge.Localization.Config.ExecutingAssembly = "Stardew Valley (multilang).exe";
                        break;
                    }
                    else if (answer == "2")
                    {
                        LocalizationBridge.Localization.Config.ExecutingAssembly = "Stardew Valley.exe";
                        break;
                    }
                }

                InjectClientSizeChangedCallback();
                InjectUpdateCallback();
                InjectLoadedGameCallback();
                InjectChangeDropDownOptionsCallback();
                InjectSetDropDownToProperValueCallback();
                InjectGetRandomNameCallback();
                InjectGetOtherFarmerNamesCallback();
                InjectParseTextCallback();
                InjectDrawObjectDialogue();
                InjectDrawObjectQuestionDialogue();
                InjectSpriteTextDrawStringCallback();
                InjectSpriteTextGetWidthOfStringCallback();
                InjectSpriteBatchDrawString();
                InjectSpriteFontMeasureString();
                InjectStringBrokeIntoSectionsCallback();
                InjectSparklingTextCallback();
                SetPatchedMark();
                GameAssembly.Write(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), LocalizationBridge.Localization.Config.ExecutingAssembly));                
            }

            StartGame(LocalizationBridge.Localization.Config.ExecutingAssembly);
        }

        static bool CheckPatchedMark()
        {
            var patchedType = GameAssembly.MainModule.GetType("StardewValley.Game1");
            var patched = patchedType.Fields.Where(f => f.FullName == "System.Boolean StardewValley.Game1::patched").Count() > 0;// (new FieldDefinition("patched", Mono.Cecil.FieldAttributes.Private, GameAssembly.MainModule.Import(typeof(bool))));
            if (patched)
            {
                return true;
            }
            else return false;
        }
        static void SetPatchedMark()
        {
            var patchedType = GameAssembly.MainModule.GetType("StardewValley.Game1");
            patchedType.Fields.Add(new FieldDefinition("patched", Mono.Cecil.FieldAttributes.Private, GameAssembly.MainModule.Import(typeof(bool))));
        }
        static void InjectClientSizeChangedCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("ClientSizeChangedCallback", new Type[] { });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var injectee = GameAssembly.GetMethod("StardewValley.Game1", "Window_ClientSizeChanged", "(System.Object,System.EventArgs)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();
            processor.InsertBefore(injecteeInstructions[0], processor.Create(OpCodes.Call, Callback));
        }
        static void InjectUpdateCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("UpdateCallback", new Type[] { });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var injectee = GameAssembly.GetMethod("StardewValley.Game1", "Update", "(Microsoft.Xna.Framework.GameTime)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var injecteeInsCount = injecteeInstructions.Count;
            var processor = injecteeBody.GetILProcessor();
            processor.InsertBefore(injecteeInstructions[injecteeInsCount - 1], processor.Create(OpCodes.Call, Callback));
        }
        static void InjectLoadedGameCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("LoadedGameCallback", new Type[] { });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var injectee = GameAssembly.GetMethod("StardewValley.Game1", "loadForNewGame", "(System.Boolean)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var injecteeInsCount = injecteeInstructions.Count;
            var processor = injecteeBody.GetILProcessor();
            processor.InsertBefore(injecteeInstructions[injecteeInsCount - 1], processor.Create(OpCodes.Call, Callback));
        }
        static void InjectChangeDropDownOptionsCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("ChangeDropDownOptionCallback", new Type[] { typeof(int), typeof(int), typeof(List<string>) });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);            

            var injectee = GameAssembly.GetMethod("StardewValley.Options", "changeDropDownOption", "(System.Int32,System.Int32,System.Collections.Generic.List`1)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var injecteeInsCount = injecteeInstructions.Count;
            var processor = injecteeBody.GetILProcessor();

            var callInstruction = processor.Create(OpCodes.Call, Callback);

            processor.InsertBefore(injecteeInstructions[0], callInstruction);
            processor.InsertBefore(callInstruction, processor.Create(OpCodes.Ldarg_1));
            processor.InsertBefore(callInstruction, processor.Create(OpCodes.Ldarg_2));
            processor.InsertBefore(callInstruction, processor.Create(OpCodes.Ldarg_3));
        }
        static void InjectSetDropDownToProperValueCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("SetDropDownToProperValueCallback", new Type[] { typeof(object) });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var injectee = GameAssembly.GetMethod("StardewValley.Options", "setDropDownToProperValue", "(StardewValley.Menus.OptionsDropDown)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var injecteeInsCount = injecteeInstructions.Count;
            var processor = injecteeBody.GetILProcessor();

            var callInstruction = processor.Create(OpCodes.Call, Callback);

            processor.InsertBefore(injecteeInstructions[0], callInstruction);
            processor.InsertBefore(callInstruction, processor.Create(OpCodes.Ldarg_1));
        }
        static void InjectGetRandomNameCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("GetRandomNameCallback", new Type[] { });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = GameAssembly.MainModule.Import(hasReturnValue.GetGetMethod());
            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = GameAssembly.MainModule.Import(eventReturnValue.GetGetMethod());

            var injectee = GameAssembly.GetMethod("StardewValley.Dialogue", "randomName", "()System.String");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            var injectionPoint = injecteeInstructions[0];
            var jmpTarget = processor.Create(OpCodes.Pop);
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Dup));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Brfalse, jmpTarget));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, eventReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
            processor.InsertBefore(injectionPoint, jmpTarget);
        }
        static void InjectGetOtherFarmerNamesCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("GetOtherFarmerNamesCallback", new Type[] { });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = GameAssembly.MainModule.Import(hasReturnValue.GetGetMethod());
            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = GameAssembly.MainModule.Import(eventReturnValue.GetGetMethod());

            var injectee = GameAssembly.GetMethod("StardewValley.Utility", "getOtherFarmerNames", "()System.Collections.Generic.List`1");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            var injectionPoint = injecteeInstructions[0];
            var jmpTarget = processor.Create(OpCodes.Pop);
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Dup));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Brfalse, jmpTarget));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, eventReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
            processor.InsertBefore(injectionPoint, jmpTarget);
        }
        static void InjectParseTextCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("ParseTextCallback", new Type[] { typeof(string), typeof(object), typeof(int) });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = GameAssembly.MainModule.Import(hasReturnValue.GetGetMethod());
            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = GameAssembly.MainModule.Import(eventReturnValue.GetGetMethod());

            var injectee = GameAssembly.GetMethod("StardewValley.Game1", "parseText", "(System.String,Microsoft.Xna.Framework.Graphics.SpriteFont,System.Int32)System.String");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            var injectionPoint = injecteeInstructions[0];
            var jmpTarget = processor.Create(OpCodes.Pop);

            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_1));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_2));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Dup));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Brfalse, jmpTarget));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, eventReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
            processor.InsertBefore(injectionPoint, jmpTarget);
        }
        static void InjectDrawObjectDialogue()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("DrawObjectDialogueCallback", new Type[] { typeof(string) });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var injectee = GameAssembly.GetMethod("StardewValley.Game1", "drawObjectDialogue", "(System.String)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            TypeReference stringType = GameAssembly.MainModule.Import(typeof(string));
            injecteeBody.Variables.Add(new VariableDefinition(stringType));
            //TypeReference listType = GameAssembly.MainModule.Import(typeof(List<>).MakeGenericType(typeof(string)));
            //injecteeBody.Variables.Add(new VariableDefinition(listType));

            //var dialogueValue = typeof(DialogueQuestion).GetProperty("Dialogue");
            //var dialogueValueImport = GameAssembly.MainModule.Import(dialogueValue.GetGetMethod());

            foreach (var instruction in injecteeInstructions.ToList())
            {
                if (instruction.OpCode == OpCodes.Ldarg_0)
                {
                    processor.Replace(instruction, processor.Create(OpCodes.Ldloc_0));
                }
            }

            var injectionPoint = injecteeInstructions.FirstOrDefault(i => i.OpCode == OpCodes.Callvirt);
            processor.InsertAfter(injectionPoint, processor.Create(OpCodes.Stloc_0));
            //processor.InsertAfter(injectionPoint, processor.Create(OpCodes.Call, dialogueValueImport));
            //processor.InsertAfter(injectionPoint, processor.Create(OpCodes.Dup));
            processor.InsertAfter(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertAfter(injectionPoint, processor.Create(OpCodes.Ldarg_0));
        }
        static void InjectDrawObjectQuestionDialogue()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("DrawObjectQuestionDialogueCallback", new Type[] { typeof(string), typeof(List<>).MakeGenericType(typeof(string)) });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var injectee = GameAssembly.GetMethod("StardewValley.Game1", "drawObjectQuestionDialogue", "(System.String,System.Collections.Generic.List`1)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            TypeReference stringType = GameAssembly.MainModule.Import(typeof(string));
            injecteeBody.Variables.Add(new VariableDefinition(stringType));
            TypeReference listType = GameAssembly.MainModule.Import(typeof(List<>).MakeGenericType(typeof(string)));
            injecteeBody.Variables.Add(new VariableDefinition(listType));

            var dialogueValue = typeof(DialogueQuestion).GetProperty("Dialogue");
            var dialogueValueImport = GameAssembly.MainModule.Import(dialogueValue.GetGetMethod());
            var choicesValue = typeof(DialogueQuestion).GetProperty("Choices");
            var choicesValueImport = GameAssembly.MainModule.Import(choicesValue.GetGetMethod());

            foreach (var instruction in injecteeInstructions.ToList())
            {
                if (instruction.OpCode == OpCodes.Ldarg_0)
                {
                    processor.Replace(instruction, processor.Create(OpCodes.Ldloc_0));
                }
                else if (instruction.OpCode == OpCodes.Ldarg_1)
                {
                    processor.Replace(instruction, processor.Create(OpCodes.Ldloc_1));
                }
            }

            var injectionPoint = injecteeInstructions[0];
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_1));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Dup));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, dialogueValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Stloc_0));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, choicesValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Stloc_1));
        }
        static void InjectSpriteTextDrawStringCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("SpriteTextDrawStringCallback",
                new Type[] {
                    typeof(object),
                    typeof(string),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(float),
                    typeof(float),
                    typeof(bool),
                    typeof(int),
                    typeof(string),
                    typeof(int)
                });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = GameAssembly.MainModule.Import(hasReturnValue.GetGetMethod());
            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = GameAssembly.MainModule.Import(eventReturnValue.GetGetMethod());

            var injectee = GameAssembly.GetMethod(
                "StardewValley.BellsAndWhistles.SpriteText",
                "drawString",
                "(Microsoft.Xna.Framework.Graphics.SpriteBatch,System.String,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Single,System.Single,System.Boolean,System.Int32,System.String,System.Int32)System.Void"
                );
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            var injectionPoint = injecteeInstructions[0];

            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_1));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_2));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_3));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 4));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 5));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 6));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 7));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 8));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 9));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 10));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 11));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 12));

            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Brfalse, injectionPoint));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
        }
        static void InjectSpriteTextGetWidthOfStringCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("SpriteTextGetWidthOfStringCallback", new Type[] { typeof(string) });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = GameAssembly.MainModule.Import(hasReturnValue.GetGetMethod());
            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = GameAssembly.MainModule.Import(eventReturnValue.GetGetMethod());

            var injectee = GameAssembly.GetMethod("StardewValley.BellsAndWhistles.SpriteText", "getWidthOfString", "(System.String)System.Int32");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            var injectionPoint = injecteeInstructions[0];
            var jmpTarget = processor.Create(OpCodes.Pop);
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Dup));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Brfalse, jmpTarget));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, eventReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Unbox_Any, injectee.ReturnType));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
            processor.InsertBefore(injectionPoint, jmpTarget);
        }
        static void InjectSpriteBatchDrawString()
        {
            var injectees = GameAssembly.Modules.SelectMany(mod => ModuleDefinitionRocks.GetAllTypes(mod))
                                       .SelectMany(t => t.Methods)
                                       .Where(method => null != method.Body).ToList();
            var FullCallbackMethod = typeof(LocalizationBridge).GetMethod("SpriteBatchDrawStringCallback", new Type[] { typeof(SpriteBatch),
            typeof(SpriteFont),typeof(string),typeof(Vector2),typeof(Color),typeof(float),typeof(Vector2),typeof(float),typeof(SpriteEffects),typeof(float)});
            var FullCallback = GameAssembly.MainModule.Import(FullCallbackMethod);
            var ShortCallbackMethod = typeof(LocalizationBridge).GetMethod("SpriteBatchDrawStringCallback", new Type[] { typeof(SpriteBatch),
            typeof(SpriteFont),typeof(string),typeof(Vector2),typeof(Color) });
            var ShortCallback = GameAssembly.MainModule.Import(ShortCallbackMethod);

            int count = 0;
            foreach (var body in injectees.Select(m => m.Body))
            {
                var processor = body.GetILProcessor();
                var instructions = body.Instructions.Where(instr => instr.OpCode == OpCodes.Callvirt && instr.ToString().Contains("SpriteBatch::DrawString")).ToList();
                foreach (var instr in instructions)
                {
                    var meth = instr.Operand as MethodReference;
                    if (meth.Parameters.Count == 9)
                    {
                        var writeInstruction = processor.Create(OpCodes.Call, FullCallback);
                        processor.Replace(instr, writeInstruction);
                    }
                    else if (meth.Parameters.Count == 4)
                    {
                        var writeInstruction = processor.Create(OpCodes.Call, ShortCallback);
                        processor.Replace(instr, writeInstruction);
                    }
                    count++;
                }
            }
        }
        static void InjectSpriteFontMeasureString()
        {
            var injectees = GameAssembly.Modules.SelectMany(mod => ModuleDefinitionRocks.GetAllTypes(mod))
                                       .SelectMany(t => t.Methods)
                                       .Where(method => null != method.Body).ToList();
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("SpriteFontMeasureStringCallback", new Type[] { typeof(SpriteFont), typeof(string) });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);
            foreach (var body in injectees.Select(m => m.Body))
            {
                var processor = body.GetILProcessor();
                var instructions = body.Instructions.Where(instr => instr.OpCode == OpCodes.Callvirt && instr.ToString().Contains("SpriteFont::MeasureString")).ToList();
                foreach (var instr in instructions)
                {
                    var meth = instr.Operand as MethodReference;
                    var writeInstruction = processor.Create(OpCodes.Call, Callback);
                    processor.Replace(instr, writeInstruction);
                }
            }

        }
        static void InjectStringBrokeIntoSectionsCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("StringBrokeIntoSectionsCallback", new Type[] { typeof(string), typeof(int), typeof(int) });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = GameAssembly.MainModule.Import(hasReturnValue.GetGetMethod());
            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = GameAssembly.MainModule.Import(eventReturnValue.GetGetMethod());

            var injectee = GameAssembly.GetMethod("StardewValley.BellsAndWhistles.SpriteText", "getStringBrokenIntoSectionsOfHeight", "(System.String,System.Int32,System.Int32)System.Collections.Generic.List`1");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            var injectionPoint = injecteeInstructions[0];
            var jmpTarget = processor.Create(OpCodes.Pop);
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_1));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_2));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Dup));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Brfalse, jmpTarget));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, eventReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
            processor.InsertBefore(injectionPoint, jmpTarget);
        }
        static void InjectSparklingTextCallback()
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("SparklingTextCallback", new Type[] { typeof(string) });
            var Callback = GameAssembly.MainModule.Import(CallbackMethod);

            var sparklingTextClass = GameAssembly.MainModule.GetType("StardewValley.BellsAndWhistles.SparklingText");
            var injectee = sparklingTextClass.Methods.First(m => m.IsConstructor && m.Parameters.Count == 9);
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            TypeReference stringType = GameAssembly.MainModule.Import(typeof(string));
            injecteeBody.Variables.Add(new VariableDefinition(stringType));

            foreach (var instruction in injecteeInstructions.ToList())
            {
                if (instruction.OpCode == OpCodes.Ldarg_2)
                {
                    processor.Replace(instruction, processor.Create(OpCodes.Ldloc_0));
                }
            }

            var injectionPoint = injecteeInstructions[0];
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_2));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Stloc_0));           
        }

        static void StartGame(string name)
        {
            var assembly = Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), name));
            assembly.EntryPoint.Invoke(null, new object[] { new string[] { } });
        }
    }   
    
    public static class CecilUtils
    {
        public static string DescriptionOf(MethodDefinition md)
        {
            var sb = new StringBuilder();
            sb.Append('(');

            var set = false;
            foreach (var param in md.Parameters)
            {
                sb.Append(param.ParameterType.Resolve().FullName);
                sb.Append(',');
                set = true;
            }
            if (set) sb.Length -= 1;

            sb.Append(')');
            sb.Append(md.ReturnType.Resolve().FullName);
            return sb.ToString();
        }

        public static bool IsGettingField(Instruction ins)
        {
            return ins.OpCode == OpCodes.Ldfld || ins.OpCode == OpCodes.Ldflda;
        }

        public static bool IsPuttingField(Instruction ins)
        {
            return ins.OpCode == OpCodes.Stfld;
        }

        public static bool IsNativeType(string returnName)
        {
            return returnName.Equals(typeof(long).FullName) ||
                returnName.Equals(typeof(ulong).FullName) ||
                returnName.Equals(typeof(int).FullName) ||
                returnName.Equals(typeof(uint).FullName) ||
                returnName.Equals(typeof(short).FullName) ||
                returnName.Equals(typeof(ushort).FullName) ||
                returnName.Equals(typeof(byte).FullName) ||
                returnName.Equals(typeof(bool).FullName);
        }

        public static bool IsJump(OpCode oc)
        {
            return
                oc == OpCodes.Br || oc == OpCodes.Br_S ||
                oc == OpCodes.Brtrue || oc == OpCodes.Brtrue_S ||
                oc == OpCodes.Brfalse || oc == OpCodes.Brfalse_S ||
                oc == OpCodes.Bne_Un || oc == OpCodes.Bne_Un_S ||
                oc == OpCodes.Blt_Un || oc == OpCodes.Blt_Un_S ||
                oc == OpCodes.Ble_Un || oc == OpCodes.Ble_Un_S ||
                oc == OpCodes.Bge_Un || oc == OpCodes.Bge_Un_S ||
                oc == OpCodes.Bgt_Un || oc == OpCodes.Bge_Un_S ||
                oc == OpCodes.Beq || oc == OpCodes.Beq_S ||
                oc == OpCodes.Ble || oc == OpCodes.Ble_S ||
                oc == OpCodes.Blt || oc == OpCodes.Blt_S
                ;
        }

        public static MethodDefinition GetMethod(this AssemblyDefinition asm, string type, string name, string desc)
        {
            var tds = asm.Modules.Where(m => m.GetType(type) != null).Select(m => m.GetType(type));
            if (tds.Count() == 0)
            {
                return null;
            }
            if (tds.Count() != 1)
            {
                throw new Exception();
            }
            var td = tds.First();
            return td.Methods.FirstOrDefault(m => m.Name.Equals(name) && DescriptionOf(m).Equals(desc.Replace(" ", string.Empty)));
        }
    }
}
