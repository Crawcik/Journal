using FlaxEngine;

namespace Journal
{
	/// <summary>
	/// Console log
	/// </summary>
	public struct ConsoleLog
	{
		/// <summary>Displayed log.</summary>
		public string Text;

		/// <summary>Log level.</summary>
		public LogType Level;

		/// <summary>Stack trace of the log.</summary>
		public string StackTrace;

		/// <summary>Constructor.</summary>
		public ConsoleLog(string text, LogType level, string stackTrace = null)
		{
			Text = text;
			Level = level;
			StackTrace = stackTrace;
		}
	}
}
