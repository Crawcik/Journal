namespace Journal
{
	/// <summary>
	/// ConsoleMap class interface through which console actor and its elements can be controled, and data passed to
	/// </summary>
	public interface IConsoleMap
	{
		/// <summary>
		/// Adds log to console
		/// </summary>    {
		void AddLog(ConsoleLog log);
		/// <summary>
		/// Requests to toogle the console
		/// </summary>    
		void Toogle(bool activate);
		/// <summary>
		/// Returns true if console is active
		/// </summary>   
		bool IsActive();
		/// <summary>
		/// Clears console from all logs. You can make it empty if you dont want to allow it
		/// </summary>
		void ClearLogs();
	}
}