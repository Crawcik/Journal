namespace Journal
{
	public readonly struct Hint
	{
		public readonly string Name;
		public readonly string Parameters;

		public Hint(string name, string parameters) : this()
		{
			this.Name = name;
			this.Parameters = parameters;
		}
	}
}