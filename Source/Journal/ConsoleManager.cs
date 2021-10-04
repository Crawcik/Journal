using System.Collections.Generic;
using FlaxEngine;

namespace Journal
{
    /// <summary>
    /// ConsoleManager Script.
    /// </summary>
    public class ConsoleManager : Script
    {
        #region Fields
        [EditorOrder(-1000)]
        public bool CreateConsoleFromPrefab;
        [EditorOrder(-990), VisibleIf("CreateConsoleFromPrefab", true)]
        public UICanvas ConsoleActor;
        [EditorOrder(-990), VisibleIf("CreateConsoleFromPrefab", false)]
        public Prefab ConsolePrefab;
        public byte MaxConsoleLogLength = 200;
        private Queue<ConsoleLog> _logs;
        private float _last = 0f;
        #endregion

        #region Properties
        public static ConsoleManager Singleton { get; private set; }
        public ConsoleMap ConsoleMap { get; private set; }
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
            if (!CheckSettings())
            {
                Enabled = false;
                return;
            }
            Singleton = this;
            _logs = new Queue<ConsoleLog>(MaxConsoleLogLength);
            Debug.Logger.LogHandler.SendLog += OnDebugLog;
#if FLAX_EDITOR
            FlaxEditor.Editor.Instance.StateMachine.PlayingState.SceneRestored += Dispose;
#endif
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            if (ConsoleActor is null)
                return;
            ConsoleActor.IsActive = false;
        }

        /// <inheritdoc/>
        public override void OnDestroy()
        {
            if (Singleton != this)
                return;
            Singleton = null;
            Dispose();
#if FLAX_EDITOR
            FlaxEditor.Editor.Instance.StateMachine.PlayingState.SceneRestored -= Dispose;
#endif
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
            }
            if (ConsoleActor is null)
            {
                Debug.LogError("Console actor is not set or cannot be spawned!");
                return false;
            }
            ConsoleMap = ConsoleActor.GetScript<ConsoleMap>();
            if (ConsoleMap is null)
            {
                Debug.LogError("Cannot find \"ConsoleMap\" script in console actor!");
                return false;
            }
            return true;
        }

        //TODO: Find better way of handling logs (by that I mean not repositioning them after max amount reached)
        private void AddLog(ConsoleLog newLog)
        {
            newLog.Spawn(ConsoleMap.OutputPanel, ConsoleMap.PanelWidth, _last, ConsoleMap.FontSize);
            _logs.Enqueue(newLog);
            _last += newLog.Label.Height + 2f;
            if (_logs.Count > MaxConsoleLogLength)
            {
                ConsoleLog oldLog = _logs.Dequeue();
                oldLog.Destroy();
                _last = 0f;
                foreach (ConsoleLog log in _logs)
                {
                    log.Label.LocalY = _last;
                    _last += log.Label.Height + 2f;
                }
            }
        }

        private void Dispose()
        {
            Debug.Logger.LogHandler.SendLog -= OnDebugLog;
            while (_logs.Count > 0)
                _logs.Dequeue().Destroy();
#if FLAX_EDITOR
            FlaxEditor.Editor.Instance.StateMachine.PlayingState.SceneRestored -= Dispose;
#endif
        }

        private void OnDebugLog(LogType level, string msg, Object obj, string stackTrace)
        {
            AddLog(new ConsoleLog(msg));
        }
        #endregion
    }
}
