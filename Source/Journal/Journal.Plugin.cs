using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Journal
{
    /// <summary>
	/// Journal plugin.
	/// </summary>
	public class MyPlugin : GamePlugin
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
            Version = new Version(0, 1),
            IsAlpha = true,
            IsBeta = false,
		};
        
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            Debug.Log("\"Journal\" enabled!");
        }

        /// <inheritdoc />
        public override void Deinitialize()
        {
            base.Deinitialize();
            Debug.Log("\"Journal\" disabled!");
        }
    }
}
