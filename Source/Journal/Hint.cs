namespace Journal
{
	/// <summary>
	/// Contains name of the command and hints about parameters
	/// </summary>
	public readonly struct Hint
	{
		/// <summary>Name of the command.</summary>
		public readonly string Name;

		/// <summary>Parameter string displaying all args and their types</summary>
		public readonly string Parameters;
		
		/// <summary>Constructor.</summary>
		public Hint(string name, string parameters) : this()
		{
			this.Name = name;
			this.Parameters = parameters;
		}
	}
}