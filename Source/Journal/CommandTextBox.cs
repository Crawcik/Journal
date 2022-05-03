using System;
using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.GUI;

namespace Journal
{
    /// <summary>
    /// InputTextBox Script.
    /// </summary>
    internal class CommandTextBox : TextBox
    {
        private List<string> _commandHistory = new List<string>();
        private int _commandHistoryIndex = -1;

        public event Action<string> OnCommand;
        public event Action OnHintChange;

        public override bool OnKeyDown(KeyboardKeys key)
        {
            switch(key)
            {
                case KeyboardKeys.Return:
                    string text = Text;
                    if (Text.StartsWith(">"))
                        text = Text.Remove(0, 1);
                    text = text.Trim();
                    if(text.Length == 0)
                        break;
                    _commandHistory.Add(text);
                    _commandHistoryIndex = _commandHistory.Count;
                    OnCommand?.Invoke(text);
                    Defocus();
                    return true;
                case KeyboardKeys.Tab:
                    OnHintChange?.Invoke();
                    return true;
                case KeyboardKeys.ArrowUp:
                    if(_commandHistoryIndex <= 0)
                        return true;
                    SetText(">" + _commandHistory[--_commandHistoryIndex]);
                    SetSelection(Text.Length, false);
                    return true;
                case KeyboardKeys.ArrowDown:
                    if(_commandHistoryIndex >= _commandHistory.Count - 1)
                        return true;
                    SetText(">" + _commandHistory[++_commandHistoryIndex]);
                    SetSelection(Text.Length, false);
                    return true;
            }
            return base.OnKeyDown(key);
        }

        public void ClearHistory()
        {
            _commandHistory.Clear();
        }
    }
}
