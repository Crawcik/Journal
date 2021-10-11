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
        private const float _baseScrollWidth = 20f;
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
        private float _lastAnimationTime;
        private float? _scrollTo;
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
        [HideInEditor, NoSerialize]
        public float Scroll
        {
            get => _outputPanel.VScrollBar.Value;
            set => _scrollTo = value;
        }
        public float PanelWidth { get; private set; }
        public int FontSize { get; private set; }
        #endregion

        #region Methods
        /// <inheritdoc/>
        public override void OnAwake()
        {
            _inputTextBox = InputTextBox?.Control as TextBox;
            _outputPanel = OutputPanel?.Control as Panel;
            if (_inputTextBox is null || _outputPanel is null)
            {
                Debug.LogError("Fields in \"Command map\" are empty!");
                Enabled = false;
                return;
            }
            _currentScreenSize = Screen.Size;
            _scrollTo = null;
            _inputTextBox.EditEnd += OnEditEnd;
            _outputPanel.VScrollBar.SizeChanged += VScrollBar_SizeChanged;
            Realign();
        }

        private void VScrollBar_SizeChanged(Control obj)
        {
            if (!_scrollTo.HasValue)
                return;
            _outputPanel.VScrollBar.Value = _scrollTo.Value;
            _scrollTo = null;
        }

        /// <inheritdoc/>
        public override void OnLateUpdate()
        {
            Vector2 screenSize = Screen.Size;
            string text = _inputTextBox.Text.Trim();
            _lastAnimationTime += Time.DeltaTime;
            if (screenSize != _currentScreenSize)
            {
                _currentScreenSize = screenSize;
                Realign();
            }
            if (_lastAnimationTime >= 1f)
            {
                _lastAnimationTime -= 1f;

                //Console waiting animation
                if (text == ">" || text == string.Empty)
                    _inputTextBox.Text = ">_";
                else if (text == ">_")
                    _inputTextBox.Text = ">";
            }
            if (_inputTextBox.IsEditing)
            {
                if (_inputTextBox.Text == ">_")
                    _inputTextBox.SetText(">");

                //Checking if '>' wasn't removed
                if (text.Length == 0 || text[0] != '>')
                {
                    text = text.TrimStart();
                    text = text.TrimEnd('>');
                    _inputTextBox.SetText(">" + text);
                    _inputTextBox.SelectionRange = new TextRange(text.Length + 1, text.Length + 1);
                }
            }
        }

        /// <summary>
        /// Aligning all UI elements
        /// </summary>
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

            PanelWidth = _currentScreenSize.X;
            FontSize = fontSize;
        }

        private void OnEditEnd()
        {
            if (Input.GetKeyDown(KeyboardKeys.Return))
            {
                Debug.Log("Command NOW!");
                _inputTextBox.SetText(">");
            }
        }
        #endregion
    }
}
