using FlaxEngine;
using FlaxEngine.GUI;
using System.Collections.Generic;
using System.Linq;

namespace Journal
{
	/// <summary>
	/// ConsoleMap Script.
	/// </summary>
#if FLAX_1_2_OR_NEWER || FLAX_1_2 || FLAX_1_3 || FLAX_1_4 || FLAX_1_5
	[Category("Journal")]
#endif
	public class BasicConsoleMap : Script, IConsoleMap
	{
		#region Constants
		private const float _baseInputHeight = 10f;
		private const float _baseScrollWidth = 15f;
		private const int _baseFontSize = 5;
		private const int _baseMaxHints = 16;
		#endregion
	
		#region Fields
		/// <summary>If true shows hint box</summary>
		[EditorOrder(-950)]
		public bool ShowHints = true;
		private bool reallign;
		private FontAsset _fontAsset;
		private FontReference _font;
		private Queue<LogEntry> _logs;
		private IEnumerable<Hint> _hintList;
		private Vector2 _currentScreenSize;
		private float _last = 0f;
		private float _lastAnimationTime;
		private bool _readOnly = false;
		private int _hintSelectIndex = -1;

		// UI
		private UIControl _inputUIControl;
		private UIControl _outputUIControl;
		private UIControl _scrollBarUIControl;
		private UIControl _scrollBarGripUIControl;
		private UIControl _hintBoxUIControl;
		private VerticalPanel _hintBoxPanel;

		// UI sizes
		private float _consoleHeight = 0.4f;
		private float _uiScale = 2.0f;
		private float _outputHeight;
		private string _prevTextFrame;
		private string _textFrame;
		#endregion

		#region Properties
		/// <summary>Input field box.</summary>
		[EditorOrder(-1000)]
		public UIControl InputField 
		{ 
			get => _inputUIControl;
			set 
			{
				if (_inputUIControl == value)
					return;
				if (!(value is null || value.Control is null || value.Control is TextBox))
				{
					Debug.LogWarning("InputField can only be a \"TextBox\" control");
					return;
				}
				_inputUIControl = value;
			}
		}

		/// <summary>Output panel UIControl.</summary>
		[EditorOrder(-990)]
		public UIControl OutputPanel
		{ 
			get => _outputUIControl;
			set 
			{
				if (_outputUIControl == value)
					return;
				if (!(value is null || value.Control is null || value.Control is ScrollableControl))
				{
					Debug.LogWarning("OutputPanel can only be a \"ScrollableControl\" control");
					return;
				}
				_outputUIControl = value;

			}
		}

		/// <summary>Scroll bar UIControl.</summary>
		[EditorOrder(-980)]
		public UIControl ScrollBar
		{ 
			get => _scrollBarUIControl;
			set 
			{
				if (_inputUIControl == value)
					return;
				if (!(value is null || value.Control is null || value.Control is Spacer))
				{
					Debug.LogWarning("ScrollBar can only be a \"Spacer\" control");
					return;
				}
				_scrollBarUIControl = value;
			}
		}

		/// <summary>Gives how many entries can console panel hold. Dont set it to too high number or game might start lagging.</summary>
		[EditorOrder(-975), Range(0, 500)]
		public int MaxConsoleLogCount = 200;

		/// <summary>Console height percentage, from 0 to 100%.</summary>
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

		/// <summary>Determines how much console and its elements will be scaled (like font size)</summary>
		[EditorOrder(-960), ShowInEditor, Range(1, 8)]
		public float UIScale
		{
			get => _uiScale;
			set
			{
				_uiScale = value;
				_font = new FontReference(_fontAsset, _baseFontSize * _uiScale);
				Realign();
			}
		}

		/// <summary>Indicates is it read only. Input box is hidden or disabled.</summary>
		[EditorOrder(-950), ShowInEditor]
		public bool ReadOnly
		{
			get => _readOnly;
			set
			{
				_readOnly = value;
				Realign();
			}
		}

		/// <summary>Output panels current view position.</summary>
		[HideInEditor, NoSerialize]
		public float ScrollPosition 
		{
			get => -OutputPanelControl.ViewOffset.Y;
			set => OutputPanelControl.ViewOffset = new Vector2(0f, -value);
		}
		
		/// <summary>Font that is used in input box and entries.</summary>
		[EditorOrder(-965), ShowInEditor]
		public FontAsset Font 
		{
			get => _fontAsset;
			set
			{
				if (_fontAsset is null && value is object)
				{
					_fontAsset = value;
					_font = new FontReference(value, _baseFontSize * _uiScale);
					Realign();
				}

			}
		}

		/// <summary>Returns width of the output panel.</summary>
		public float PanelWidth => OutputPanelControl.Width;

		private Spacer ScrollBarControl => (Spacer)_scrollBarUIControl.Control;
		private TextBox InputTextBox => (TextBox)_inputUIControl.Control;
		private ScrollableControl OutputPanelControl => (ScrollableControl)_outputUIControl.Control;
		#endregion

		#region Methods
		/// <inheritdoc/>
		public override void OnAwake()
		{
			/*
			_inputTextBox = InputTextBox?.Control as TextBox;
			_outputPanel = OutputPanel?.Control as ScrollableControl;
			_scrollBar = ScrollBar?.Control;
			*/
			reallign = false;
			_prevTextFrame = ">";
			if (OutputPanelControl is null)
			{
				Debug.LogError("Fields in \"Command map\" are empty!");
				Enabled = false;
				return;
			}
			if (InputTextBox is object)
			{
				_hintList = new List<Hint>();
				_hintBoxUIControl = new UIControl {
					Name = "Hints",
					Parent = Actor,
					Control = _hintBoxPanel = new VerticalPanel {
						Visible = false,
						AutoSize = false,
						BackgroundColor = InputTextBox.BackgroundColor + new Color(30, 30, 30, 0),
						Pivot = new Vector2(0f, 0f),
					}
				};
				InputTextBox.TextChanged += OnTextChanged;
				if (InputTextBox is CommandTextBox commandTextBox)
				{
					commandTextBox.OnCommand += OnCommand;
					commandTextBox.OnHintChange += OnHintChange;
				}
				else
				{
					InputTextBox.EditEnd += OnEditEnd;
				}
			}
			if (ScrollBarControl is object)
			{
				_scrollBarGripUIControl = ScrollBar.AddChildControl<Spacer>();
				Control control = _scrollBarGripUIControl.Control;
				control.BackgroundColor = ScrollBarControl.BackgroundColor + new Color(30, 30, 30, 0);
				control.Pivot = Vector2.Zero;
				control.LocalLocation = Vector2.Zero;
				RealignScrollBar();
			}
			_logs = new Queue<LogEntry>(MaxConsoleLogCount);
			_currentScreenSize = Screen.Size;
			Realign();
		}

		/// <inheritdoc/>
        public override void OnUpdate()
        {
			// This is used for checking if on close keyboard key made an addition to input box when closing it.
			// Closing from ConsoleManager is on LateUpdate so we save textframe in Update.
            _prevTextFrame = _textFrame;
        }

		/// <inheritdoc/>
		public override void OnLateUpdate()
		{
			Vector2 screenSize = Screen.Size;
#if FLAX_EDITOR
			screenSize /= FlaxEditor.Editor.Instance.Options.Options.Interface.InterfaceScale;
#endif
			float scrollDelta = Input.MouseScrollDelta;
			_textFrame = InputTextBox.Text;
			_lastAnimationTime += Time.DeltaTime;
			if (OutputPanelControl.IsMouseOver && scrollDelta != 0f)
			{
				ScrollPosition =  Mathf.Clamp(ScrollPosition - scrollDelta * 20f, 0f, _last - _outputHeight);
				if (ScrollBarControl != null)
					RealignScrollBar();
			}
			if (screenSize != _currentScreenSize)
			{
				_currentScreenSize = screenSize;
				Realign();
			}
			if (_lastAnimationTime >= 1f)
			{
				_lastAnimationTime -= 1f;

				//Console waiting animation
				if (_textFrame == ">" || _textFrame == string.Empty)
					InputTextBox.Text = ">_";
				else if (_textFrame == ">_")
					InputTextBox.Text = ">";
			}
			if (InputTextBox.IsFocused)
			{
				if (_textFrame == ">_" || _textFrame.Length == 0)
					InputTextBox.SetText(">");
				//Checking if '>' wasn't misplaced
				if (!_textFrame.StartsWith('>'))
				{
					_textFrame = _textFrame.TrimStart();
					_textFrame = _textFrame.TrimEnd('>');
					InputTextBox.SetText(">" + _textFrame);
					InputTextBox.SelectionRange = new TextRange(_textFrame.Length + 1, _textFrame.Length + 1);
				}
				_textFrame = InputTextBox.Text;
				var select = InputTextBox.SelectionRange;
				if (select.StartIndex < -1 || select.EndIndex < 1) // Fix for bugged select
					InputTextBox.SelectionRange = new TextRange(_textFrame.Length + 1, _textFrame.Length + 1);
			}
			else
			{
				_hintBoxPanel.Visible = false;
			}
			if (ScrollBarControl is null)
				return;
			if (_scrollBarGripUIControl.IsActive)
			{
				Control control = _scrollBarGripUIControl.Control;
				if (Input.Mouse.GetButton(MouseButton.Left) && control.IsMouseOver)
				{
					float offset = (control.Size / 2f).Y;
					float position = (Input.MousePosition - (control.Pivot * control.Size)).Y;
					position = Mathf.Clamp(position, offset, _outputHeight - offset) - offset;
					ScrollPosition = Mathf.Remap(position + offset, offset, _outputHeight - offset, 0f, _last - _outputHeight);
					_scrollBarGripUIControl.Position = new Vector3(_scrollBarGripUIControl.Position.X, position, 0f);
				}
			}
		}

		/// <summary>
		/// Alignings all UI elements
		/// </summary>
		public void Realign()
		{
			if (OutputPanelControl is null)
				return;
			float containerHeight = _currentScreenSize.Y * _consoleHeight;
			float inputHeight = _readOnly ? 0f : (_baseInputHeight * _uiScale);
			float scrollBarWidth = _baseScrollWidth * (_uiScale * 0.75f);
			float outputWidth = _currentScreenSize.X - scrollBarWidth;
			if(_font is object)
			{
				InputTextBox.Font = _font;
				_font.Size = _baseFontSize * _uiScale;
			}
			_outputHeight = containerHeight - inputHeight;

			if (_readOnly || InputTextBox is null)
			{
				InputTextBox.Visible = false;
			}
			else
			{
				InputTextBox.Visible = true;
				InputTextBox.Location = new Vector2(0f, _outputHeight);
				InputTextBox.Size = new Vector2(_currentScreenSize.X, inputHeight);
				_hintBoxPanel.Location = new Vector2(0f, _outputHeight - _hintBoxPanel.Height);
			}
			OutputPanelControl.Location = Vector2.Zero;
			OutputPanelControl.Size = new Vector2(outputWidth, _outputHeight);
			RealignLogs(true);

			if (ScrollBarControl is null)
				return;
			ScrollBarControl.Location = new Vector2(outputWidth, 0f);
			ScrollBarControl.Size = new Vector2(scrollBarWidth, _outputHeight);
			RealignScrollBar();
		}

		/// <inheritdoc/>
		public void Toogle(bool activate)
		{
			Actor.IsActive = activate;
			if (activate)
			{
				InputTextBox.SetText(_prevTextFrame); // Reverting text, toogle key made it to input box. Rought solution but better than nothing.
				InputTextBox.Focus();
				return;
			}
			InputTextBox.Defocus();
		}

		/// <inheritdoc/>
		public bool IsActive() => Actor.IsActive;

		//TODO: Find better way of handling logs (by that I mean not repositioning them after max amount reached)
		/// <inheritdoc/>
		public void AddLog(ConsoleLog log)
		{
			var newLog = new LogEntry(log.Text, log.Level);
			newLog.Spawn(OutputPanel, PanelWidth, _last, _font);
			_logs.Enqueue(newLog);
			_last += newLog.Label.Height + 2f;
			if (_logs.Count > MaxConsoleLogCount)
			{
				LogEntry oldLog = _logs.Dequeue();
				oldLog.Destroy();
				RealignLogs();
			}
			float scrollPos = ScrollPosition;
			float limit = _last - _outputHeight;
			if(scrollPos < limit)
				ScrollPosition = limit;
			RealignScrollBar();
		}

		/// <inheritdoc/>
		public void ClearLogs()
		{
			while (_logs.Count > 0)
				_logs.Dequeue().Destroy();
		}

		private void RealignScrollBar()
		{
			if (_scrollBarGripUIControl is null)
				return;
			if (_last <= OutputPanelControl.Height)
			{
				_scrollBarGripUIControl.IsActive = false;
				return;
			}
			_scrollBarGripUIControl.IsActive = true;
			Control control = _scrollBarGripUIControl.Control;
			control.LocalY = ScrollBarControl.Height * (ScrollPosition / _last);
			control.Width = ScrollBarControl.Width;
			control.Height = ScrollBarControl.Height * (OutputPanelControl.Height / _last);
		}

		private void RealignLogs(bool widthChange = false)
		{
			_last = 0f;
			float width = OutputPanelControl.Width;
			//Double check if width change is worth it
			if (_logs.Count > 0 && widthChange)
				widthChange = !Mathf.Approximately(_logs.Peek().Label.Width, width);
			foreach (LogEntry log in _logs)
			{
				log.Label.LocalY = _last;
				log.Label.Font.Size = _baseFontSize * _uiScale;
				if (widthChange)
					log.Label.Width = width;
				_last += log.Label.Height + 2f;
			}
		}
		#endregion

		#region Event Handlers
		private void OnTextChanged()
		{
			if (InputTextBox is null || _hintBoxUIControl is null || _readOnly || !ShowHints)
				return;
			_hintSelectIndex = -1;
			if (InputTextBox.Text == ">" || InputTextBox.Text == ">_" || InputTextBox.Text.Length < 2)
			{
				_hintList = new List<Hint>();
				_hintBoxPanel.Visible = false;
				return;
			}
			string text = InputTextBox.Text.Remove(0, 1);
			IEnumerable<Hint> commands = ConsoleManager.Singleton.Commands
				.Where(x => x.Name.StartsWith(text))
				.Select(x => new Hint(x.Name, string.Join(" ", x.Parameters
					.Select(y => $"[{y.Name}: {y.ParameterType.Name}]")
				))).OrderBy(x => x.Name);
			bool refresh = commands.Except(_hintList).Any() || _hintList.Except(commands).Any();
			_hintList = commands;
			if (!refresh)
				return;
			if (!commands.Any())
			{
				_hintBoxPanel.Visible = false;
				return;
			}
			_hintBoxPanel.DisposeChildren();
			Label longestLabel = null;
			int i = 0;
			foreach (Hint command in commands)
			{
				HintLabel label = _hintBoxPanel.AddChild<HintLabel>();
				label.Font = _font;
				label.AutoWidth = true;
				label.AutoHeight = true;
				label.HorizontalAlignment = TextAlignment.Near;
				label.SetHint(command);
				label.TextColorHighlighted = Color.Yellow;
				label.Clicked += x => {
					InputTextBox.SetText(">" + x);
					InputTextBox.SelectionRange = new TextRange(x.Length + 1, x.Length + 1);
				};
				longestLabel = longestLabel ?? label;
				if (label.Text.Value.Length > longestLabel.Text.Value.Length)
					longestLabel = label;
				i++;
				if (i >= _baseMaxHints / _uiScale)
					break;
			}
			Vector2 size = longestLabel.Font.GetFont().MeasureText(longestLabel.Text);
			_hintBoxPanel.Width = size.X + 2f;
			_hintBoxPanel.Height = i * (longestLabel.Height + _hintBoxPanel.Spacing);
			_hintBoxPanel.Location = new Vector2(0f, _outputHeight - _hintBoxPanel.Height);
			_hintBoxPanel.Visible = true;
		}

		private void OnHintChange()
		{
			_hintSelectIndex++;
			if (_hintSelectIndex >= _hintList.Count())
				_hintSelectIndex = 0;
			_hintBoxPanel.Children[_hintSelectIndex].OnMouseEnter(Vector2.Zero);
			_hintBoxPanel.Children[(_hintSelectIndex == 0 ? _hintList.Count() : _hintSelectIndex) - 1].OnMouseLeave();
		}

		private void OnEditEnd()
		{
			if (!Input.GetKeyDown(KeyboardKeys.Return))
				return;
			Debug.Log(InputTextBox.Text);
			try
			{
				ConsoleTools.SeparateCommandAndArgs(InputTextBox.Text, out var command, out var args, offset: 1);
				ConsoleManager.ExecuteCommand(command, args);
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning(ex.Message);
			}
			InputTextBox.SetText(">");
		}


		private void OnCommand(string commandText)
		{
			if (_hintSelectIndex > 0)
			{
				string text = ">" + ((HintLabel)_hintBoxPanel.Children[_hintSelectIndex]).HintText;
				InputTextBox.SetText(text);
				_hintSelectIndex = -1;
				InputTextBox.SelectionRange = new TextRange(text.Length, text.Length);
				return;
			}
			Debug.Log(InputTextBox.Text);
			try
			{
				ConsoleTools.SeparateCommandAndArgs(commandText, out var command, out var args, offset: 0);
				ConsoleManager.ExecuteCommand(command, args);
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning(ex.Message);
			}
			InputTextBox.SetText(">");
		}

		#endregion

		#region Structures
		/// <summary>
		/// Log entry handle class
		/// </summary>
		public class LogEntry
		{
			/// <summary>Displayed log.</summary>
			public readonly string Text;

			/// <summary>Log level.</summary>
			public readonly LogType Level;

			private UIControl _uiElement;

			/// <summary>
			/// UI label reference.
			/// </summary>
			public Label Label { get; private set; }

			/// <summary>Constructor.</summary>
			public LogEntry(string text, LogType level)
			{
				Text = text;
				Level = level;
			}

			internal void Spawn(UIControl parent, float width, float y, FontReference font)
			{
				if (_uiElement is object || parent is null)
					return;
				Label = new Label(0f, 0f, width, 0f)
				{
					Font = font,
					Text = new LocalizedString(Text),
					TextColor = GetColor(),
					HorizontalAlignment = TextAlignment.Near,
					VerticalAlignment = TextAlignment.Center,
					AutoHeight = true,
					Margin = new Margin(3f),
					AutoFitText = false,
					Pivot = new Vector2(0f, 0f),
					BackgroundColor = new Color(0, 0, 0, 40)
				};
				_uiElement = Object.New<UIControl>();
				_uiElement.Control = Label;
				_uiElement.Parent = parent;
				_uiElement.LocalPosition = new Vector3(0f, y, 0f);
			}

			internal void Destroy()
			{
				if (_uiElement is null)
					return;
				Object.Destroy(_uiElement);
			}

			private Color GetColor()
			{
				switch(Level) 
				{
					case LogType.Warning:   return Color.Yellow;
					case LogType.Error:     return Color.Red;
					case LogType.Fatal:     return Color.DarkRed;
					default:                return Color.White;
				}
			}
		}
		#endregion
	}
}
