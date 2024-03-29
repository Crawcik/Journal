﻿using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.GUI;

namespace Journal
{
	/// <summary>
	/// ConsoleManager Script.
	/// </summary>
	public class ConsoleManager : Script
	{
		#region Fields
		[EditorOrder(-1000)]
		public bool CreateConsoleFromPrefab = true;
		[EditorOrder(-990), VisibleIf("CreateConsoleFromPrefab", true)]
		public UICanvas ConsoleActor;
		[EditorOrder(-990), VisibleIf("CreateConsoleFromPrefab", false)]
		public Prefab ConsolePrefab;
		[EditorOrder(-985)]
		public FontAsset Font;
		[EditorOrder(-980)]
		public KeyboardKeys OpenCloseButton = KeyboardKeys.BackQuote;
		[EditorOrder(-970)]
		public bool DontDestroyOnLoad = false;
		[EditorOrder(-960)]
		public bool HeadlessConsole = false;
		private List<Command> _commands;
		#endregion

		#region Properties
		public static bool IsOpen => Singleton.Map.Actor.IsActive;
		public static ConsoleManager Singleton { get; private set; }
		public ConsoleMap Map { get; private set; }
		internal IReadOnlyList<Command> Commands => _commands;
		
		#endregion

		#region Methods
		/// <inheritdoc/>
		public override void OnAwake()
		{
			if (Singleton is object)
			{
				Debug.LogWarning("Multiple instances of command manager script found! Destroying additional instances.");
				Destroy(this);
				return;
			}
			if (DontDestroyOnLoad && this.Scene.Name != "DontDestroyOnLoad")
			{
				var scene = new Scene()
				{
					Name = "DontDestroyOnLoad",
					StaticFlags = StaticFlags.FullyStatic
				};
				var bytes = Level.SaveSceneToBytes(scene, prettyJson: false);
				scene = Level.LoadSceneFromBytes(bytes);
				this.Actor.Parent = scene;
			}
			if (!CheckSettings())
			{
				Enabled = false;
				return;
			}
			Map.Font = Font;
			Map.Actor.IsActive = false;
			Singleton = this;
			_commands = new List<Command>();
			RegisterCommand("help", Help);
			RegisterCommand<string>("echo", Debug.Log);
			RegisterCommand("exit", () => Engine.RequestExit(0));
			RegisterCommand("clear", () => Map.Clear());
			Debug.Logger.LogHandler.SendLog += OnDebugLog;
#if FLAX_EDITOR
			FlaxEditor.Editor.Instance.StateMachine.PlayingState.SceneRestored += Dispose;
#endif
		}

		/// <inheritdoc/>
		public override void OnLateUpdate()
		{
			if(Input.GetKeyDown(OpenCloseButton))
				Map.Actor.IsActive = !Map.Actor.IsActive;
		}

		/// <inheritdoc/>
		public override void OnDisable()
		{
			if (ConsoleActor is null)
				return;
			ConsoleActor.IsActive = false;
		}

		/// <inheritdoc/>
		public override void OnDestroy()
		{
			if (Singleton != this)
				return;
			Singleton = null;
			Dispose();
#if FLAX_EDITOR
			FlaxEditor.Editor.Instance.StateMachine.PlayingState.SceneRestored -= Dispose;
#endif
		}

		/// <summary>
		/// Executes command with specified name... who would have guess
		/// </summary>
		/// <param name="name">The command name</param>
		/// <param name="args">Parameters of the command</param>
		public static void ExecuteCommand(string name, params string[] args)
		{
			
			object[] paramArray = args is null ? null : new object[args.Length];
			Command command = Singleton._commands.FirstOrDefault(x =>
			{
				if (x.Name != name || x.Parameters.Length != args.Length)
					return false;
				try
				{
					for (int i = 0; i < args.Length; i++)
						paramArray[i] = Convert.ChangeType(args[i], x.Parameters[i].ParameterType);
				}
				catch { return false; }
				return true;
			});
			if(command is null)
			{
				Debug.LogError("Command not found!");
				return;
			}
			try
			{
				command.MethodInfo.Invoke(command.Target, paramArray);
			}
			catch (Exception exception)
			{
				Debug.LogError(exception);
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
			if(Singleton is null)
			{
				Debug.LogError("Register command was called too early or command manager doesnt exist!");
				return;
			}
			name = name.Replace(' ', '_');
			if (Singleton._commands.Any(x => x.Name == name && x.Parameters.Length == method.GetParameters().Length))
				Debug.LogWarning($"Command with this name \"{name}\" and exact parameters count already exists.");
			else
				Singleton._commands.Add(new Command(name, method, target));
		}

		/// <summary>
		/// Unregisters all commands with specified name
		/// </summary>
		/// <param name="name">The command name</param>
		public static void UnregisterCommand(string name)
		{
			List<Command> commands = Singleton._commands.FindAll(x => x.Name == name);
			if(commands.Count == 0)
			{
				Debug.LogWarning($"Command with name \"{name}\" doesn't exists.");
				return;
			}
			commands.ForEach(x => Singleton._commands.Remove(x));
		}

		private bool CheckSettings()
		{
			if(Engine.IsHeadless)
				HeadlessConsole = true;
			if(HeadlessConsole)
				return true;
			if (CreateConsoleFromPrefab)
			{
				if (ConsolePrefab is null)
				{
					Debug.LogError("Console prefab is not set!");
					return false;
				}
				ConsoleActor = (UICanvas)PrefabManager.SpawnPrefab(ConsolePrefab, Scene);
			}
			if (ConsoleActor is null)
			{
				Debug.LogError("Console actor is not set or cannot be spawned!");
				return false;
			}
			Map = ConsoleActor.GetScript<ConsoleMap>();
			if (Map is null)
			{
				Debug.LogError("Cannot find \"ConsoleMap\" script in console actor!");
				return false;
			}
			return true;
		}

		private void Help()
		{
			IEnumerable<Command> sorted = _commands.OrderBy(x=>x.Name);
			Debug.Log($"List of all commands:");
			foreach(Command command in sorted)
			{
				string paramsText = "";
				Array.ForEach(command.Parameters, x => paramsText += $" {x.Name}:{x.ParameterType.Name}");
				Debug.Log(" --> " + command.Name + paramsText);
			}
		}

		//Destroys all logs created by Object.New<T> to minimalize crash propability 
		private void Dispose()
		{
			Debug.Logger.LogHandler.SendLog -= OnDebugLog;
			Map?.Clear();
#if FLAX_EDITOR
			FlaxEditor.Editor.Instance.StateMachine.PlayingState.SceneRestored -= Dispose;
#endif
		}

		private void OnDebugLog(LogType level, string msg, FlaxEngine.Object obj, string stackTrace) => Map.AddLog(new ConsoleLog(msg, level));
		#endregion

		internal class Command 
		{
			public readonly string Name;
			public readonly MethodInfo MethodInfo;
			public readonly ParameterInfo[] Parameters;
			public readonly object Target;

			public Command(string name, MethodInfo methodInfo, object target)
			{
				this.Name = name;
				this.MethodInfo = methodInfo;
				this.Parameters = methodInfo.GetParameters();
				this.Target = target;
			}
		}
	}
}
