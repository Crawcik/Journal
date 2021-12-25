# Journal - Console for Flax Engine with command handling

## How can I implement this in my project?
1. Download this project .zip or .tar.gz
2. Put in folder near your project (for example `<your-flax-project-path>/Plugins/Journal/`)
3. Add in your `.flaxproj` file path to plugin, like in this example:
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
4. Go to <your-flax-project-path>/Source/Game/Game.Build.cs and add "**Journal**" module, here is example: *if you don't want to use commands this is optional*
```cs
public override void Setup(BuildOptions options)
{
  base.Setup(options);
  
  options.PrivateDependencies.Add("Journal"); // Adds reference to Journal types
}
```
5. If something doesn't work: check logs, try deleting `Cache` folder or generate project files manually
  
Also here is official tutorial for installing plugins: https://docs.flaxengine.com/manual/scripting/plugins/plugin-project.html
  
## How to use console?
1. In Flax Editor go to `Journal/Source/Journal/` and place `ConsoleManager.cs` script in the scene where u want (for example in **Scene** actor)
2. Go to `Journal/Content/` drag `ConsolePrefab` into "ConsolePrefab" field in script
3. If you want to customize console UI:
   1. Go to `Journal/Content/` in editor and drag `ConsolePrefab` on to the scene
   2. In ConsoleManager script set "CreateConsoleFromPrefab" to false
  
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
      ConsoleManager.RegisterCommand("quit", Quit); // <---
    }
  }
  
  public void Quit()
  {
    //Do something here and quit
  }
}
```
