using System;
using System.Reflection;
using FlaxEngine;

namespace Journal
{
	/// <summary>
	/// Journal plugin.
	/// </summary>
	internal class Journal : GamePlugin
	{
		private readonly Version _version = new Version(1, 2);

		/// <inheritdoc />
		#if FLAX_1_4_OR_NEWER || FLAX_1_4 || FLAX_1_5
		public Journal() : base()
		{
			var _descriptionLines = new string[] {
				"Console & Command handling plugin.",
			};
			_description = new PluginDescription()
			{
				Name = "Journal",
				Category = "Utility",
				Author = "Crawcik",
				AuthorUrl = "https://github.com/Crawcik",
				HomepageUrl = "https://github.com/Crawcik/Journal",
				RepositoryUrl = "https://github.com/Crawcik/Journal",
				Description = string.Join('\n',_descriptionLines),
				Version = _version,
				IsAlpha = false,
				IsBeta = false,
			};
		}
		#else
		public override PluginDescription Description => new PluginDescription()
		{
			Name = "Journal",
			Category = "Utility",
			Author = "Crawcik",
			AuthorUrl = "https://github.com/Crawcik",
			HomepageUrl = "https://github.com/Crawcik/Journal",
			RepositoryUrl = "https://github.com/Crawcik/Journal",
			Description = "Console with command handling for Flax Engine",
			Version = _version,
			IsAlpha = false,
			IsBeta = false,
		};
		#endif
		
		/// <summary>
		/// Registers command with specified name and execution method in given command group
		/// This overload is not recomended!
		/// </summary>
		/// <param name="name">The command name</param>
		/// <param name="method">The method info</param>
		/// <param name="target">The method info</param>
		internal static void RegisterCommand(string name, MethodInfo method, object target) => ConsoleManager.RegisterCommand(name, method, target);

		/// <summary>
		/// Unregisters all commands with specified name
		/// </summary>
		/// <param name="name">The command name</param>
		internal static void UnregisterCommand(string name) => ConsoleManager.UnregisterCommand(name);
	}
}
