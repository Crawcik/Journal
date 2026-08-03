#if PLATFORM_LINUX || PLATFORM_WINDOWS || PLATFORM_MACOSs
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
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
		private float _blinkTime;
		private bool _blinkCurrentlyDisplayed;
		private bool _refreshInput;
		#endregion

		#region Properties
		#endregion

		/// <summary>Constructor.</summary>
		public HeadlessConsoleMap()
		{
			_terminalThread = new Thread(TerminalSetup);
			_command = new StringBuilder(70);
			_bag = new ConcurrentQueue<ConsoleLog>();
			_blinkTime = 0.0f;
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
			if (activate && !_active)
			{
				_terminalThread.Start();
				Scripting.Update += OnUpdate;
			}
			else
			{
				Scripting.Update -= OnUpdate;
				if (_terminalThread.IsAlive)
					_terminalThread.Join();
			}

			_active = activate;
			
		}

		private void OnUpdate()
		{
			_blinkTime += Time.DeltaTime;
			if (_blinkTime > 2.0f)
				_blinkTime -= 2.0f;

			// Would use JobSystem but I want to preserve compability with Flax 1.0
			if (!_terminalThread.IsAlive && _active)
			{
				_terminalThread = new Thread(HandleTerminal);
				_terminalThread.Start();
			}
		}

		// Run only once to test the console capabilities
		private void TerminalSetup()
		{
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
				// if set doesnt work then get wont help much so we disable both of them
				_terminalCursorSet = false;
				_terminalCursorGet = false;
			}
			try
			{
				_terminalSizeGet = Console.WindowHeight > 0;
			}
			catch (Exception)
			{
				_terminalSizeGet = false;
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
			// This is temporary to check in different terminals
			string[] data = new string[] {
				"CursorSet: " + _terminalCursorSet,
				"CursorGet: " + _terminalCursorGet,
				"ColorSupport: " + _terminalColorSupport,
				"SizeGet: " + _terminalSizeGet
			};
			Console.WriteLine(string.Join(',', data));
			
			InputRestore();
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
				int newPos;
				char[] buffer;

				switch (keyInfo.Key)
				{
					case ConsoleKey.RightArrow:
					case ConsoleKey.LeftArrow:
						if (!_terminalCursorSet || !_terminalCursorGet)
							break;
						_commandCursorPos += keyInfo.Key == ConsoleKey.LeftArrow ? -1 : 1;
						newPos = Math.Clamp(_commandCursorPos, 0,  _command.Length);
						if (_commandCursorPos != newPos)
							_commandCursorPos = newPos; // Out of range
						else 
							Console.SetCursorPosition(newPos + 2, Console.GetCursorPosition().Item2);
						break;
					// TODO: selecting historic command
					case ConsoleKey.UpArrow: break;
					case ConsoleKey.DownArrow: break;
					// TODO: hint select systemx
					case ConsoleKey.Tab: break;
					case ConsoleKey.Enter:
						// TODO: confirming use of selected historic command
						try
						{
							// This was we skip "Debug.Logger.LogHandler.SendLog" event firing and dont create feedback loop.
							// It goes to C++ part, so file logger mostly.
							Debug.Logger.LogHandler.LogWrite(LogType.Info, "> " + _command.ToString());

							ConsoleTools.SeparateCommandAndArgs(_command.ToString(), out var command, out var args);
							BlinkRefresh(true);
							Console.WriteLine(); // Because command text is written already, we just jump to new line
							_command.Clear();
							InputRestore(); // We restore input to nothing
							ConsoleManager.ExecuteCommand(command, args);
						}
						catch (Exception ex)
						{
							// Error while parsing command, we stash it (reset input line) and write error, then restore
							_refreshInput = false;
							InputStash();
							WriteLogTerminal(new ConsoleLog(ex.Message, LogType.Warning));
							InputRestore();
						}
						break;
					case ConsoleKey.Backspace:
						if (_command.Length == 0)
							break;
						if (_terminalCursorSet && _commandCursorPos < _command.Length)
						{
							_commandCursorPos--;
							newPos = _command.Length - _commandCursorPos;
							buffer = new char[newPos + 3];
							buffer[0] = '\b';
							buffer[1] = ' ';
							buffer[2] = '\b';
							buffer[newPos + 2] = ' '; // So the last char will not be left hanging in console
							_command.Remove(_commandCursorPos, 1);
							_command.CopyTo(_commandCursorPos, buffer, 3, newPos - 1);
							Console.Write(buffer);
							Console.SetCursorPosition(_commandCursorPos + 2, Console.GetCursorPosition().Item2);
							break;
						}
						Console.Write("\b \b");
						_command.Length -= 1;
						_commandCursorPos--;
						break;
					default:
						if (char.IsControl(chr))
							break;
						if (_commandCursorPos == _command.Length)
						{
							// Simple char append
							_command.Append(chr);
							Console.Write(chr);
						}
						else 
						{
							_command.Insert(_commandCursorPos, chr);
							_refreshInput = true; 
							// We do an whole input refresh instead of moving & rewriting in console text,
							// because if someone pastes long string then we do rewrite for each char.
						}
						_commandCursorPos++;
						break;
				};
			}
			
			if (_blinkCurrentlyDisplayed == _blinkTime > 1.0f)
			{
				_blinkCurrentlyDisplayed = !_blinkCurrentlyDisplayed;
				BlinkRefresh();
			}

			// If true then input should be refreshed at the end so it displays properly
			if (_refreshInput)
			{
				_refreshInput = false;
				InputStash();
				InputRestore();
			}
		}

		// We restore input to the line in the way it was previously displayed
		private void InputRestore()
		{
			if (_blinkTime > 1.0f)
				_blinkCurrentlyDisplayed = false;
			WriteBlinkCursor();
			Console.Write(_command.ToString());
			if (_terminalCursorSet && _terminalCursorGet && _command.Length != _commandCursorPos)
			{
				_commandCursorPos = Math.Min(_command.Length, _commandCursorPos); // Just so it is correct
				Console.SetCursorPosition(_commandCursorPos + 2, Console.GetCursorPosition().Item2);
			}
			else _commandCursorPos = _command.Length;
		}

		// Here we try to get rid of input line, so the incoming log wont be missplaced
		private void InputStash()
		{
			if (_terminalCursorSet && _terminalCursorGet)
			{
				// Incoming log or input replacement might be shorter than input so we fix that by overwriting its content with spaces.
				ValueTuple<int,int> curPos = Console.GetCursorPosition();
				Console.SetCursorPosition(0, curPos.Item2);
				Console.Write(new string(' ', _command.Length + 2));
			}
			Console.Write('\r');
		}

		private void BlinkRefresh(bool forceShow = false)
		{
			if (_terminalCursorSet && _terminalCursorGet)
			{
				ValueTuple<int,int> curPos = Console.GetCursorPosition();
				Console.SetCursorPosition(0, curPos.Item2);
				WriteBlinkCursor(forceShow);
				Console.SetCursorPosition(curPos.Item1, curPos.Item2);
				return;
			}
			// This method might flicker in some circumstances
			_refreshInput = false;
			InputStash();
			InputRestore();
		}

		// Only run when cursor at beggining
		private void WriteBlinkCursor(bool forceShow = false)
		{
			if (_terminalColorSupport)
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(BlinkString(forceShow));
				Console.ForegroundColor = ConsoleColor.White;
			}
			else Console.Write(BlinkString(forceShow));
		}

		// If cursor pos can be set arrow blink will occur (good to have it to see if main flax thread freezed)
		private string BlinkString(bool forceShow = false) =>
			(!forceShow &&_terminalCursorSet && _terminalCursorGet && _blinkTime > 1.0f)
				? "  "
				: "> ";

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