using System;
using FlaxEngine;

namespace Journal
{
    /// <summary>
	/// Journal plugin.
	/// </summary>
	internal class PluginInfo : GamePlugin
    {
        #if FLAX_1_3 || FLAX_1_2 || FLAX_1_1 || FLAX_1_0
		/// <inheritdoc />
		public override PluginDescription Description => new PluginDescription()
        {
            Name = "Journal",
            Category = "Utility",
            Author = "Crawcik",
            AuthorUrl = "https://github.com/Crawcik",
            HomepageUrl = "https://github.com/Crawcik/Journal",
            RepositoryUrl = "https://github.com/Crawcik/Journal",
            Description = "Console with command handling for Flax Engine",
            Version = new Version(1, 1),
            IsAlpha = false,
            IsBeta = false,
		};
        #else
        public PluginInfo() : base()
		{
            _description = new PluginDescription()
            {
                Name = "Journal",
                Category = "Utility",
                Author = "Crawcik",
                AuthorUrl = "https://github.com/Crawcik",
                HomepageUrl = "https://github.com/Crawcik/Journal",
                RepositoryUrl = "https://github.com/Crawcik/Journal",
                Description = "Console with command handling for Flax Engine",
                Version = new Version(1, 1),
                IsAlpha = false,
                IsBeta = false,
            };
        }
        #endif
    }
}
