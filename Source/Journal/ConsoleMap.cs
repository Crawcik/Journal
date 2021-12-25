using FlaxEngine;
using FlaxEngine.GUI;
using System.Collections.Generic;

namespace Journal
{
    /// <summary>
    /// ConsoleMap Script.
    /// </summary>
    public class ConsoleMap : Script
    {
        #region Constants
        private const float _baseInputHeight = 10f;
        private const float _baseScrollWidth = 8f;
        private const int _baseFontSize = 5;
        #endregion

        #region Fields
        [EditorOrder(-1000)]
        public UIControl InputTextBox;
        [EditorOrder(-990)]
        public UIControl OutputPanel;
        [EditorOrder(-980)]
        public UIControl ScrollBar;
        public byte MaxConsoleLogCount = 200;
        private Queue<ConsoleLog> _logs;
        private TextBox _inputTextBox;
        private ScrollableControl _outputPanel;
        private Control _scrollBar;
        private UIControl _scrollBarGrip;
        private Vector2 _currentScreenSize;
        private float _last = 0f;
        private float _lastAnimationTime;
        private bool _readOnly = false;

        //UI sizes
        private float _consoleHeight = 0.4f;
        private int _uiScale = 2;
        private float _outputHeight;
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
        [EditorOrder(-960), ShowInEditor]
        public bool ReadOnly
        {
            get => _readOnly;
            set
            {
                _readOnly = value;
                Realign();
            }
        }
        [HideInEditor, NoSerialize]
        public float ScrollPosition 
        {
            get => -_outputPanel.ViewOffset.Y;
            set => _outputPanel.ViewOffset = new Vector2(0f, -value);
        }
        public float PanelWidth => _outputPanel.Width;
        public int FontSize { get; private set; }
        #endregion

        #region Methods
        /// <inheritdoc/>
        public override void OnStart()
        {
            _inputTextBox = InputTextBox?.Control as TextBox;
            _outputPanel = OutputPanel?.Control as ScrollableControl;
            _scrollBar = ScrollBar?.Control;
            if (_inputTextBox is null || _outputPanel is null)
            {
                Debug.LogError("Fields in \"Command map\" are empty!");
                Enabled = false;
                return;
            }
            if (_scrollBar is object)
            {
                _scrollBarGrip = ScrollBar.AddChildControl<Spacer>();
                Control control = _scrollBarGrip.Control;
                control.BackgroundColor = _scrollBar.BackgroundColor + new Color(30, 30, 30, 0);
                control.Pivot = Vector2.Zero;
                control.LocalX = 0f;
                control.LocalY = 0f;
                RealignScrollBar();
            }
            _logs = new Queue<ConsoleLog>(MaxConsoleLogCount);
            _inputTextBox.EditEnd += OnEditEnd;
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
            if (_scrollBar is null)
                return;
            if (_scrollBarGrip.IsActive)
            {
                Control control = _scrollBarGrip.Control;
                if (Input.Mouse.GetButton(MouseButton.Left) && control.IsMouseOver)
                {
                    float offset = (control.Size / 2f).Y;
                    float position = (Input.MousePosition - (control.Pivot * control.Size)).Y;
                    position = Mathf.Clamp(position, offset, _outputHeight - offset) - offset;
                    ScrollPosition = Mathf.Map(position + offset, offset, _outputHeight - offset, 0f, _last - _outputHeight);
                    _scrollBarGrip.Position = new Vector3(_scrollBarGrip.Position.X, position, 0f);
                }
            }
        }

        /// <summary>
        /// Aligning all UI elements
        /// </summary>
        public void Realign()
        {
            if (_inputTextBox is null || _outputPanel is null)
                return;
            float containerHeight = _currentScreenSize.Y * _consoleHeight;
            float inputHeight = _readOnly ? 0f : (_baseInputHeight * _uiScale);
            float scrollBarWidth = _baseScrollWidth * _uiScale;
            float outputWidth = _currentScreenSize.X - scrollBarWidth;
            int fontSize = _baseFontSize * _uiScale;
            _outputHeight = containerHeight - inputHeight;

            if (_readOnly)
            {
                _inputTextBox.Visible = false;
            }
            else
            {
                _inputTextBox.Visible = true;
                _inputTextBox.X = 0f;
                _inputTextBox.Y = _outputHeight;
                _inputTextBox.Width = _currentScreenSize.X;
                _inputTextBox.Height = inputHeight;
                _inputTextBox.Font.Size = fontSize;
            }
            _outputPanel.X = 0f;
            _outputPanel.Y = 0f;
            _outputPanel.Width = outputWidth;
            _outputPanel.Height = _outputHeight;
            FontSize = fontSize;
            RealignLogs(true);

            if (_scrollBar is null)
                return;
            _scrollBar.X = outputWidth;
            _scrollBar.Y = 0f;
            _scrollBar.Width = scrollBarWidth;
            _scrollBar.Height = _outputHeight;
            RealignScrollBar();
        }


        //TODO: Find better way of handling logs (by that I mean not repositioning them after max amount reached)
        public void AddLog(ConsoleLog newLog)
        {
            newLog.Spawn(OutputPanel, PanelWidth, _last, FontSize);
            _logs.Enqueue(newLog);
            _last += newLog.Label.Height + 2f;
            if (_logs.Count > MaxConsoleLogCount)
            {
                ConsoleLog oldLog = _logs.Dequeue();
                oldLog.Destroy();
                RealignLogs();
            }
            float scrollPos = ScrollPosition;
            float limit = _last - _outputHeight;
            if(scrollPos < limit)
                ScrollPosition = limit;
            RealignScrollBar();
        }

        private void RealignScrollBar()
        {
            if (_scrollBarGrip is null)
                return;
            if (_last <= _outputPanel.Height)
            {
                _scrollBarGrip.IsActive = false;
                return;
            }
            _scrollBarGrip.IsActive = true;
            Control control = _scrollBarGrip.Control;
            control.LocalY = _scrollBar.Height * (ScrollPosition / _last);
            control.Width = _scrollBar.Width;
            control.Height = _scrollBar.Height * (_outputPanel.Height / _last);
        }

        private void RealignLogs(bool widthChange = false)
        {
            _last = 0f;
            float width = _outputPanel.Width;
            //Double check if width change is worth it
            if (_logs.Count > 0 && widthChange)
                widthChange = !Mathf.Approximately(_logs.Peek().Label.Width, width);
            foreach (ConsoleLog log in _logs)
            {
                log.Label.LocalY = _last;
                log.Label.Font.Size = _baseFontSize * _uiScale;
                if (widthChange)
                    log.Label.Width = width;
                _last += log.Label.Height + 2f;
            }
        }

        public void Clear()
        {
            while (_logs.Count > 0)
                _logs.Dequeue().Destroy();
        }

        private void OnEditEnd()
        {
            if (Input.GetKeyDown(KeyboardKeys.Return))
            {
                _inputTextBox.SetText(">");
            }
        }
        #endregion
    }
}
