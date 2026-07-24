# <p align="center">Journal</p>
## Console for Flax Engine with command handling
[![GitHub release](https://img.shields.io/github/release/Crawcik/Journal?style=for-the-badge)](https://github.com/Crawcik/Journal/releases)
[![Github license](https://img.shields.io/github/license/Crawcik/Journal?style=for-the-badge)](https://github.com/Crawcik/Journal/blob/master/LICENSE.md)

* [How to use](#how-to-use-console)
* [Adding commands](#how-to-add-commands-to-console)
* [Shortcuts](#shortcuts)
* [Installation](#installation)

## How to use console?
1. In Flax Editor go to `Journal/Source/Journal/` and place `ConsoleManager.cs` script in the scene where u want (for example in **Scene** actor)
2. Go to `Journal/Content/` drag `ConsolePrefab` into "ConsolePrefab" field in script
3. If you want to customize console UI:
   1. Go to `Journal/Content/` in editor and drag `ConsolePrefab` on to the scene
   2. In ConsoleManager script set `CreateConsoleFromPrefab` unchecked
   3. Drag console actor to `ConsoleActor` field
4. After starting game press open key (by default "~") and use the console
  
## How to add commands to console?
Like this:
```cs
using FlaxEngine;
using Journal;

namespace Game
{
	public class Test : Script 
	{
 		public override void OnStart() 
		{
			ConsoleManager.RegisterCommand("hello", Hello); // <---
		}
  
		public void Hello()
		{
			Debug.Log("Hello, world!");
			// Do something here
		}
	}
}
```
## Shortcuts for default consoles
- **Tab**: Selecting hints when editing
- **Arrow Up**: Get previous commands
- **Arrow Down**: Get recent commands

## Installation

### After Flax 1.7
- Go to Tools->Plugins in Editor. In a window press "Clone Project".

### Before Flax 1.7
- A) Download this project .zip or .tar.gz and unpack it in folder near your project (for example `<your-flax-project-path>/Plugins/Journal/`) 
- B) Clone the repo. Command: `git clone https://github.com/Crawcik/Journal.git`
- Add in your `.flaxproj` file path to plugin, like in this example:
```json
{
	"GameTarget": "GameTarget",
	"EditorTarget": "GameEditorTarget",
	"References": [
		{
			"Name": "$(EnginePath)/Flax.flaxproj"
		},
		{
			"Name": "$(ProjectPath)/Plugins/Journal/Journal.flaxproj"
		}
	],
}
```

### With Flax Plugin Manager:
- Download, unpack & run **Flax Plugin Manager** [[Click here](https://github.com/Crawcik/FlaxPluginManager/releases/latest)]
- Select your project & add Journal

## Setup (referencing)

### Simple
Go to `<your-flax-project-path>/Source/Game/Game.Build.cs` and add "**Journal**" module, here is example: *if you don't want to use commands this is optional*
```cs
public override void Setup(BuildOptions options)
{
	base.Setup(options);

	options.PrivateDependencies.Add("Journal"); // Adds reference to Journal types
}
```
If something doesn't work: check logs, try deleting `Cache` folder or generate project files manually

Also here is official tutorial for installing plugins: https://docs.flaxengine.com/manual/scripting/plugins/plugin-project.html

### Adaptive (prefered for other plugins)
This method is better if you want to integrate "**Journal**" in your plugin, but you want to have it optional (no need to have Journal installed).

Copy `<journal-project-path>/Content/JournalExtension.cs` to desired module source code folder. You can now use functions in the `Journal` class ***without*** referencing Journal in `.flaxproj`
```cs
public override void OnStart()
{
    Journal.RegisterCommand<int>("is_odd", IsOdd);
}

public static void IsOdd(int number)
{
    Debug.Log("This number is" + number%2==0 ? "odd" : "not odd");
}
```
The commands will appear, if **Journal** is referenced if *any other* project, like top project! If not, dont worry. Your code will still compile without it being present!