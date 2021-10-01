using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Journal
{
    /// <summary>
    /// ConsoleManager Script.
    /// </summary>
    public sealed class ConsoleManager : Script
    {
        #region Fields
        [EditorOrder(-1000)]
        public bool CreateConsoleFromPrefab;
        [EditorOrder(-990), VisibleIf("CreateConsoleFromPrefab", true)]
        public UICanvas ConsoleActor;
        [EditorOrder(-990), VisibleIf("CreateConsoleFromPrefab", false)]
        public Prefab ConsolePrefab;
        #endregion

        #region Properties
        public static ConsoleManager Singleton { get; private set; }
        #endregion

        #region Methods
        /// <inheritdoc/>
        public override void OnStart()
        {
            if (Singleton is object)
            {
                Debug.LogWarning("Multiple instances of command manager script found! Destroying additional instances");
                Destroy(this);
                return;
            }
            Singleton = this;

            if (!CheckSettings())
            {
                Enabled = false;
                return;
            }
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            if (ConsoleActor is null)
                return;
            ConsoleActor.IsActive = false;
        }

        public override void OnDestroy()
        {
            if (Singleton == this)
                Singleton = null;
        }

        private bool CheckSettings()
        {
            if (CreateConsoleFromPrefab)
            {
                if (ConsolePrefab is null)
                {
                    Debug.LogError("Console prefab is not set!");
                    return false;
                }
                ConsoleActor = (UICanvas)PrefabManager.SpawnPrefab(ConsolePrefab);
                if (ConsoleActor is null)
                {
                    Debug.LogError("Console cannot be spawned!");
                    return false;
                }
                return true;
            }
            if (ConsoleActor is null)
            {
                Debug.LogError("Console actor is not set!");
                return false;
            }
            return true;
        }
        #endregion
    }
}
