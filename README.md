# Stardew Valley Localization Tool

## Description:

Hello everyone. We are two developers: [WarFollowsMe](https://github.com/WarFollowsMe) and [Curiouspers](https://github.com/curiouspers)

We very like Stardew Valley, but real excitement comes only when you play the game comfortably with your native language.
This is what pushed us forward to make **Stardew Valley Localization** to our native language - Russian. As we dig deeper and deeper into code, we realized that **we can make this tool multilingual, and capable to handle other language packages**.
All of this became possible, when we decided to not to change original texts in game resources, but create a mechanism to replace texts right before rendering stage. So the game thinks that it working with original texts, but our tool acts like a little build-up on top of the game, participating only at texts rendering stage, and don't change the logic of the game. 

Here's what result looks like in the game:
![](http://take.ms/uoh8p)

Well, it took us three month and now we want to share the results of our work with you, the great community of Stardew Valley!

**WARNING!** This is not the end release, but pretty stable version. Bugs can occur here and there. The main goal right now is to figure out is there any interest to this project.

Here is the source code: https://github.com/WarFollowsMe/TranslationMod

The tool itself consist of two parts:
1. LanguagePatcher.exe - a little console program, that makes a series of injections to the game with help of Mono.Cecil library.

2. MultiLanguage.dll - library, that contains methods to process intercepted texts and replacing them with localized texts from custom made dictionary.

All language packages are placed in folder "languages", that consist of:

    -[languages]
      Config.json           // required 
	  description.json      // required
	  [EN]                  // required folder
	  [RU]             	    // optional folder
	  ...		            // maybe your language folder here

1. Config.json - simple configuration file. Contains only two properties:
	- LanguageName - short language name that will be set when you launch the game, example: "RU"
	- ExecutingAssembly - well, name of executable file, example: "Stardew Valley.exe"


2. description.json - configuration file in format "ShortLanguageName":"FullLanguageName"
Must always contain "EN":"English"! Other pairs are optional.

3. Subfolders are language packages. Name of subfolders must always match the ShortLanguageName field in description.json
Subfolder EN must always be there.

Language package must always contain 2 subfolders: "content" and "dictionaries"

Folder "content" keeps all .xnb files with textures, that may be redrawn to other languages. 
Folder structure replicates the in game Content folder.

Folder "dictionaries" keeps all dictionaries in JSON format. That's where you want to place the translations of original texts. Use the following format: "original text":"translated text". 
We split texts to different files on purpose. That way it's easier to orient in all of these texts. 
Dictionaries contains special lines, started with "__comment", that serves the same purpose, to make it clearer from what part of the game the following texts are. 

Also it's very important to understand that, dictionaries with filename started with underscore sign ("_") are contains texts that was hardcoded into "Stardew Valley.exe".
Since in the game a lot of attention is paid to random generated dialogues and quests, we added complicated system of variables, that's very easy to use. 
Now it's only 5 variables:

* @key - placeholder for text that will be replaced by other (original) key from any dictionary
* @number - placeholder for numbers, floats and time
* @player - players name
* @farm - name of player's farm
* @playerChild - name of any player child

*It is possible that we may put new variables some day.*

### Now, how to install it? 

1. You can download compiled archive from here: http://bit.ly/SV_TR, *or* compile it yourself from source code.
2. List of files and folders that you must copy in root game folder:
	- [languages]
	- LanguagePatcher.exe
	- Mono.Cecil.dll
	- Mono.Cecil.Mdb.dll
	- Mono.Cecil.Pdb.dll
	- Mono.Cecil.Rocks.dll
	- MultiLanguage.dll
	- ICSharpCode.SharpZipLib.dll
	- Cyriller.dll
	- Newtonsoft.Json.dll
3. Launch LanguagePatcher.exe. This program will check if your game patched, if not it will offer to patch the game. Press "y", then press "Enter".
Then you can choose: enter "1" to create a new file, named "Stardew Valley(multilang).exe" or "2" to patch original file. In second case you must backup original "Stardew Valley.exe" **beforehand**.
Then you must see the message: "Patch successfully applied. Enjoy!".
That's all, you can start the game.


### How to add your language package:
1. In the "languages" folder make a copy of "EN" folder and rename it, for example to "RU"
2. Open descriptions.json and add another line, for example: "RU":"Russian"
3. Translate every dictionary from dictionaries folder.
4. You can use xnbNode to unpack and pack .xnb files with images that may be translated. 
(last one is optional, if you like english text on textures, that's ok)

To activate a new language package, you need to change "LanguageName" value in Config.json, or you may start a game and at the options page there will be new "Language" dropdown, you may also select your language there. 


![Language option](https://monosnap.com/file/TtKeEllUUUvBrlHqs6gNDmGVyrBeD0.png)

**IMPORTANT! Changes will take place only after game restart.**

### Using mods. 
* **SMAPI** supported

You can test any mod tool at your own risk.
*Just keep in mind that at the time we do not prioritize this task.*


## Developers: 
* [WarFollowsMe](https://github.com/WarFollowsMe)
* [CuriousPers](https://github.com/curiouspers)


### If you liked this project you can donate to us, we really appreciate it:

[![Paypal](http://icons.iconarchive.com/icons/chrisbanks2/cold-fusion-hd/128/paypal-icon.png)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=VN9VEYTE6LUNG)
[![Yandex.Money](http://lh3.googleusercontent.com/oS19Ky9c7603VTx2aAnqFZRsy3PELsAOPngXlRRX1yA10WYZdHBK9ID8juuCY96mdlwR=w128)](https://money.yandex.ru/to/410012434852933)

![WebMoney](https://www.webmoney.ru/img/new/logo-wm.png)
* Z128309117755
* R134563815660




### Licence: 
* **GNU LESSER GENERAL PUBLIC LICENSE**
* Read **LICENSE** file for more details
