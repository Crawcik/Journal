using System;
using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.GUI;

namespace Journal
{
    /// <summary>
    /// ConsoleMap Script.
    /// </summary>
    public class ConsoleMap : Script
    {
        #region Constants
        private const float _baseInputHeight = 10f;
        private const int _baseFontSize = 5;
        #endregion

        #region Fields
        [EditorOrder(-1000)]
        public UIControl InputTextBox;
        [EditorOrder(-990)]
        public UIControl OutputPanel;
        [EditorOrder(-980)]
        public UIControl ScrollBar;
        private TextBox _inputTextBox;
        private Panel _outputPanel;
        private Vector2 _currentScreenSize;
        private float _consoleHeight = 0.4f;
        private int _uiScale = 2;
        #endregion

        #region Properties
        [EditorOrder(-970), ShowInEditor, Range(0, 100), Space(5f)]
        public int ConsoleHeightPercent 
        {
            get => (int)(_consoleHeight * 100f);
            set
            {
                _consoleHeight = value / 100f;
                Realign();
            }
        }
        [EditorOrder(-970), ShowInEditor, Range(1, 8)]
        public int UIScale
        {
            get => _uiScale;
            set
            {
                _uiScale = value;
                Realign();
            }
        }
        #endregion

        /// <inheritdoc/>
        public override void OnAwake()
        {
            _currentScreenSize = Screen.Size;
            _inputTextBox = InputTextBox?.Control as TextBox;
            _outputPanel = OutputPanel?.Control as Panel;
            if (_inputTextBox is null || _outputPanel is null)
            {
                Debug.LogError("Fields in \"Command map\" are empty!");
                Enabled = false;
                return;
            }
            Realign();
        }

        public void Realign()
        {
            float containerHeight = _currentScreenSize.Y * _consoleHeight;
            float inputHeight = _baseInputHeight * _uiScale;
            float outputHeight = containerHeight - inputHeight;
            int fontSize = _baseFontSize * _uiScale;

            _inputTextBox.X = 0f;
            _inputTextBox.Y = outputHeight;
            _inputTextBox.Width = _currentScreenSize.X;
            _inputTextBox.Height = inputHeight;
            _inputTextBox.Font.Size = fontSize;
            _outputPanel.X = 0f;
            _outputPanel.Y = 0f;
            _outputPanel.Width = _currentScreenSize.X;
            _outputPanel.Height = outputHeight;
        }

        /// <inheritdoc/>
        public override void OnLateUpdate()
        {
            Vector2 screenSize = Screen.Size;
            if(screenSize != _currentScreenSize)
            {
                _currentScreenSize = screenSize;
                Realign();
            }
        }
    }
}
