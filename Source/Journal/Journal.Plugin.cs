using System;
using FlaxEngine;

namespace Journal
{
    /// <summary>
	/// Journal plugin.
	/// </summary>
	internal class PluginInfo : GamePlugin
    {
		/// <inheritdoc />
		public override PluginDescription Description => new PluginDescription
        {
            Name = "Journal",
            Category = "Console",
            Author = "Crawcik",
            AuthorUrl = "https://github.com/Crawcik",
            HomepageUrl = "https://github.com/Crawcik/Journal",
            RepositoryUrl = "https://github.com/Crawcik/Journal",
            Description = "Console with command handling for Flax Engine",
            Version = new Version(1, 0),
            IsAlpha = false,
            IsBeta = false,
		};
    }
}
