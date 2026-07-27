//
// This is a script that you can copy to "Source" in your game or plugin.
// If some flax project will add Journal, then you will be able to add commands to Journal console
// even if you dont specify reference in ".Build.cs" file.
// Don't worry, if Journal wont be present no error or exception will be thrown.
// Just drag and drop this to "Source" dir, use it and if Journal present commands will be added
//
// Example use:
//--------------------------------------
//using FlaxEngine;
//
//public class TestScript : Script
//{
//    public override void OnEnable()
//    {
//        Journal.RegisterCommand("game_test", Test);
//    }
//    
//    public override void OnDisable()
//    {
//        Journal.UnregisterCommand("game_test");
//    }
//    
//    public void Test()
//    {
//         Debug.Log("Test");
//    }
//} 
//--------------------------------------
//

using System;
using System.Reflection;
using FlaxEngine;

/// <summary>
/// Journal plugin external interface.
/// Allows to register and unregister commands for Journal console if present.
/// </summary>
internal sealed class Journal
{
	private static Journal _singleton;
	private readonly JPluginMap? _journal;

	static Journal()
	{
		try
		{
			_singleton = new Journal();
		}
		catch (Exception exception)
		{
			_singleton = null;
			Debug.LogError(exception);
		}
	}

	private Journal()
	{
		var journalPluginInstance = PluginManager.GetPlugin("Journal");
		if (journalPluginInstance != null)
		{
			// Plugin found!
			var type = journalPluginInstance.GetType();
			var registerCommand = type.GetMethod("RegisterCommand", BindingFlags.Static | BindingFlags.NonPublic, new[] { typeof(string), typeof(MethodInfo),  typeof(object)});
			var unregisterCommand = type.GetMethod("UnregisterCommand", BindingFlags.Static | BindingFlags.NonPublic, new[] { typeof(string)});
			if (registerCommand is null || unregisterCommand is null)
				throw new Exception("Journal is incompatible with plugin. Check if implementation changed");
			_journal = new JPluginMap {
				Instance = journalPluginInstance,
				RegisterCommand = registerCommand,
				UnregisterCommand = unregisterCommand
			};
		}
	}

	/// <summary>
	/// Registers command with specified name and execution method in given command group
	/// </summary>
	/// <param name="name">The command name</param>
	/// <param name="method">Called when command is being executed</param>
	public static void RegisterCommand(string name, Action method) => RegisterCommand(name, method.Method, method.Target);

	/// <summary>
	/// Registers command with specified name and execution method in given command group
	/// </summary>
	/// <param name="name">The command name</param>
	/// <param name="method">Called when command is being executed</param>
	public static void RegisterCommand<T>(string name, Action<T> method) => RegisterCommand(name, method.Method, method.Target);

	/// <summary>
	/// Registers command with specified name and execution method in given command group
	/// </summary>
	/// <param name="name">The command name</param>
	/// <param name="method">Called when command is being executed</param>
	public static void RegisterCommand<T1, T2>(string name, Action<T1, T2> method) => RegisterCommand(name, method.Method, method.Target);

	/// <summary>
	/// Registers command with specified name and execution method in given command group
	/// </summary>
	/// <param name="name">The command name</param>
	/// <param name="method">Called when command is being executed</param>
	public static void RegisterCommand<T1, T2, T3>(string name, Action<T1, T2, T3> method) => RegisterCommand(name, method.Method, method.Target);

	/// <summary>
	/// Registers command with specified name and execution method in given command group
	/// </summary>
	/// <param name="name">The command name</param>
	/// <param name="method">Called when command is being executed</param>
	public static void RegisterCommand<T1, T2, T3, T4>(string name, Action<T1, T2, T3, T4> method) => RegisterCommand(name, method.Method, method.Target);

	/// <summary>
	/// Registers command with specified name and execution method in given command group
	/// This overload is not recomended!
	/// </summary>
	/// <param name="name">The command name</param>
	/// <param name="method">The method info</param>
	/// <param name="target">The method info</param>
	public static void RegisterCommand(string name, MethodInfo method, object target)
	{
		if (_singleton == null || _singleton._journal == null)
			return;
		_singleton._journal.Value.RegisterCommand.Invoke(null, new object[] { name, method, target });
	}

	/// <summary>
	/// Unregisters all commands with specified name
	/// </summary>
	/// <param name="name">The command name</param>
	public static void UnregisterCommand(string name)
	{
		if (_singleton == null || _singleton._journal == null)
			return;
		_singleton._journal.Value.UnregisterCommand.Invoke(null, new object[] { name });
	}

	private struct JPluginMap {
		public Plugin Instance;
		public MethodInfo RegisterCommand;
		public MethodInfo UnregisterCommand;
	}
}