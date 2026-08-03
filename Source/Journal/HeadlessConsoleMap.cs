#if PLATFORM_LINUX || PLATFORM_WINDOWS || PLATFORM_MACOS

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FlaxEditor;
using FlaxEngine;

namespace Journal
{
	/// <summary>
	/// Headless console handler that controlls input and output of terminal in which game was started in headless mode.
	/// Gives better formatting for outgoing logs from Flax and handling of input for command giving in a neat way.
	/// </summary>
	public class HeadlessConsoleMap : IConsoleMap
	{
		#region Fields

		// Shared fields
		private readonly ConcurrentQueue<ConsoleLog> _bag;
		private bool _terminalColorSupport  = false;
		private bool _terminalCursorSet  = false;
		private bool _terminalCursorGet  = false;
		private bool _terminalSizeGet  = false;

		// Flax threads fields
		private Thread _terminalThread;
		private bool _active = false;

		// Terminal thread-only fields
		private readonly StringBuilder _command;
		private int _commandCursorPos;

		#endregion

		#region Properties

		#endregion

		/// <summary>Constructor.</summary>
		public HeadlessConsoleMap()
		{
			_terminalThread = new Thread(HandleTerminal);
			_command = new StringBuilder(70);
			_bag = new ConcurrentQueue<ConsoleLog>();
		}

		#region Methods
		/// <inheritdoc/>
		public void AddLog(ConsoleLog log)
		{
			if (log.Level == LogType.Fatal)
			{
				Console.WriteLine(); // Just so we minimise overlap!
				WriteLogTerminal(log);
				return;
			}
			_bag.Enqueue(log);

		}

		/// <inheritdoc/>
		public void ClearLogs()
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc/>
		public bool IsActive()
		{
			return _active;
		}

		/// <inheritdoc/>
		public void Toogle(bool activate)
		{
#if !PLATFORM_LINUX && !PLATFORM_WINDOWS && !PLATFORM_MAC
			throw new NotSupportedException("How?");
#endif
			if (Engine.IsEditor)
				throw new NotSupportedException("This console can only work in headless condition!");

			if (!Engine.IsHeadless)
				return;
			if (Engine.CommandLine.Contains("-std"))
			{
				Debug.LogWarning("Journal headless console cannot work with \"-std\" option. Disabling.");
				return;
			}
			if (Console.IsOutputRedirected || Console.IsInputRedirected)
			{
				Debug.LogWarning("Journal cannot work. Programs text output or input is not going through terminal/console. Disabling.");
				return;
			}
			try
			{
				Console.Clear();
				Console.SetCursorPosition(0, 1);
				_terminalCursorSet = true;
				ValueTuple<int, int> pos = Console.GetCursorPosition();
				_terminalCursorGet = pos.Item2 == 1;
				Console.SetCursorPosition(0, 0);
			}
			catch (Exception)
			{
				_terminalCursorSet = false;
				_terminalCursorGet = false;
			}
			try
			{
				_terminalSizeGet = Console.WindowHeight > 0;
				
			}
			catch (Exception)
			{
				_terminalCursorSet = false;
				_terminalCursorGet = false;
			}
			try
			{
				Console.ForegroundColor = ConsoleColor.White; 
				// On Mac and Linux some .NET versions don't indicate if its working or not,
				// so it will change the property but in reality no color command will be send
				// to the terminal. But thats fine, just no color :D
				_terminalColorSupport = Console.ForegroundColor == ConsoleColor.White;
			}
			catch (Exception)
			{
				_terminalColorSupport = false;
			}
			if (activate)
			{
				Scripting.Update += OnUpdate;
			}
			else
			{
				Scripting.Update -= OnUpdate;
				if (_terminalThread.IsAlive)
					_terminalThread.Join();
			}

			_active = activate;
			
			string[] data = new string[] {
				"CursorSet: " + _terminalCursorSet,
				"CursorGet: " + _terminalCursorGet,
				"ColorSupport: " + _terminalColorSupport,
				"SizeGet: " + _terminalSizeGet
			};
			Console.WriteLine(string.Join(',', data));
		}

		private void OnUpdate()
		{
			// Would use JobSystem but I want to preserve compability with Flax 1.0
			if (!_terminalThread.IsAlive && _active)
			{
				_terminalThread = new Thread(HandleTerminal);
				_terminalThread.Start();
			}
		}

		// Here we handle terminal, this should not be on any of Flax's main threads to not throttle them.
		private void HandleTerminal()
		{
			// Input saving
			var wasEmpty = _bag.IsEmpty;
			if (!wasEmpty) // If there are outputs then we run 
				InputStash();

			// Output handling
			while (!_bag.IsEmpty)
			{
				if(!_bag.TryDequeue(out var log))
					continue;
				WriteLogTerminal(log);
			}
			
			if (!wasEmpty)
				InputRestore();

			// Input handling
			while(Console.KeyAvailable)
			{
				var keyInfo = Console.ReadKey(true);
				var chr = keyInfo.KeyChar;
				switch (keyInfo.Key)
				{
					// TODO: selecting historic command
					case ConsoleKey.UpArrow: break;
					case ConsoleKey.DownArrow: break;
					// TODO: hint select systemx
					case ConsoleKey.Tab: break;
					case ConsoleKey.Enter:
						// TODO: confirming use of selected historic command
						try
						{
							ConsoleTools.SeparateCommandAndArgs(_command.ToString(), out var command, out var args);
							Console.WriteLine(); // Because command text is written already, we just jump to new line
							_command.Clear();
							InputRestore(); // We restore input to nothing
							ConsoleManager.ExecuteCommand(command, args);
						}
						catch (Exception ex)
						{
							// Error while parsing command, we stash it (reset lines) and write error, then restore
							InputStash();
							WriteLogTerminal(new ConsoleLog(ex.Message, LogType.Warning));
							InputRestore();
						}
						break;
					case ConsoleKey.Backspace:
						if (_command.Length == 0)
							break;
						_command.Length -= 1;
						Console.Write("\b \b");
						break;
					default:
						if (char.IsControl(chr))
							break;
						_command.Append(chr);
						Console.Write(chr);
						_commandCursorPos++;
						break;
				};
			}
		}

		// We restore input to the line in the way it was previously displayed
		private void InputRestore()
		{
			_commandCursorPos = _command.Length;
			Console.Write(_command.ToString());
		}

		// Here we try to get rid of input line, so the incoming log wont be missplaced
		private void InputStash()
		{
			Console.Write('\r');
			_commandCursorPos = 0;
		}

		// This can be called thread-safe, not overlaping way, but in case of FATAL log we better print it asap!
		private void WriteLogTerminal(ConsoleLog log)
		{
			if (!_terminalColorSupport)
			{
				Console.WriteLine($"[{GetLogName(log)}] {log.Text}" );
				return;
			}
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write('[');
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(GetLogName(log));
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write("] ");
			Console.ForegroundColor = GetLogColor(log);
			Console.Write(log.Text);
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine();
		}

		private static string GetLogName(ConsoleLog log)
		{
			switch (log.Level)
			{
				case LogType.Info: return "INFO ";
				case LogType.Warning: return "WARN ";
				case LogType.Error: return "ERROR";
				case LogType.Fatal: return "FATAL";
				default: throw new ArgumentException("Bad ConsoleLog.Level value");
			}
		}

		private static ConsoleColor GetLogColor(ConsoleLog log)
		{
			switch (log.Level)
			{
				case LogType.Info: return ConsoleColor.Gray;
				case LogType.Warning: return ConsoleColor.Yellow;
				case LogType.Error: return ConsoleColor.Red;
				case LogType.Fatal: return ConsoleColor.DarkRed;
				default: throw new ArgumentException("Bad ConsoleLog.Level value");
			}
		}
		#endregion
	}
}
#endif