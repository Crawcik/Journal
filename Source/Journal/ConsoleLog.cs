using FlaxEngine;

namespace Journal
{
	/// <summary>
	/// Console log
	/// </summary>
	public struct ConsoleLog
	{
		public string Text;
		public LogType Level;
		// Can be null or empty!
		public string StackTrace;

		public ConsoleLog(string text, LogType level, string stackTrace = null)
		{
			Text = text;
			Level = level;
			StackTrace = stackTrace;
		}
	}
}
