using FlaxEngine;
using FlaxEngine.GUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Journal
{
	/// <summary>
	/// ConsoleMap Script.
	/// </summary>
	public class ConsoleMap : Script
	{
		#region Constants
		private const float _baseInputHeight = 10f;
		private const float _baseScrollWidth = 15f;
		private const int _baseFontSize = 5;
		private const int _baseMaxHints = 16;
		#endregion

		#region Fields
		[EditorOrder(-1000)]
		public UIControl InputTextBox;
		[EditorOrder(-990)]
		public UIControl OutputPanel;
		[EditorOrder(-980)]
		public UIControl ScrollBar;
		[EditorOrder(-977)]
		public byte MaxConsoleLogCount = 200;
		[EditorOrder(-950)]
		public bool ShowHints = true;
		private Queue<ConsoleLog> _logs;
		private IEnumerable<(string, string)> _hintList;
		private TextBox _inputTextBox;
		private ScrollableControl _outputPanel;
		private Control _scrollBar;
		private UIControl _scrollBarGrip;
		private UIControl _hintBox;
		private VerticalPanel _hintBoxControl;
		private Vector2 _currentScreenSize;
		private float _last = 0f;
		private float _lastAnimationTime;
		private bool _readOnly = false;
		private int _hintSelectIndex = -1;

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
		public override void OnAwake()
		{
			_inputTextBox = InputTextBox?.Control as TextBox;
			_outputPanel = OutputPanel?.Control as ScrollableControl;
			_scrollBar = ScrollBar?.Control;
			if (_outputPanel is null)
			{
				Debug.LogError("Fields in \"Command map\" are empty!");
				Enabled = false;
				return;
			}
			if (_inputTextBox is object)
			{
				_hintList = new List<(string, string)>();
				_hintBox = new UIControl {
					Name = "Hints",
					Parent = Actor,
					Control = _hintBoxControl = new VerticalPanel {
						Visible = false,
						AutoSize = false,
						BackgroundColor = _inputTextBox.BackgroundColor + new Color(30, 30, 30, 0),
						Pivot = new Vector2(0f, 0f),
					}
				};
				_inputTextBox.TextChanged += OnTextChanged;
				if (_inputTextBox is CommandTextBox commandTextBox)
				{
					commandTextBox.OnCommand += OnCommand;
					commandTextBox.OnHintChange += OnHintChange;
				}
				else
				{
					_inputTextBox.EditEnd += OnEditEnd;
				}
			}
			if (_scrollBar is object)
			{
				_scrollBarGrip = ScrollBar.AddChildControl<Spacer>();
				Control control = _scrollBarGrip.Control;
				control.BackgroundColor = _scrollBar.BackgroundColor + new Color(30, 30, 30, 0);
				control.Pivot = Vector2.Zero;
				control.LocalLocation = Vector2.Zero;
				RealignScrollBar();
			}
			_logs = new Queue<ConsoleLog>(MaxConsoleLogCount);
			_currentScreenSize = Screen.Size;
			Realign();
		}

		/// <inheritdoc/>
		public override void OnLateUpdate()
		{
			Vector2 screenSize = Screen.Size;
			#if FLAX_EDITOR
			screenSize /= FlaxEditor.Editor.Instance.Options.Options.Interface.InterfaceScale;
			#endif
			string text = _inputTextBox.Text.Trim();
			float scrollDelta = Input.MouseScrollDelta;
			_lastAnimationTime += Time.DeltaTime;
			if (_outputPanel.IsMouseOver && scrollDelta != 0f)
			{
				ScrollPosition =  Mathf.Clamp(ScrollPosition - scrollDelta * 20f, 0f, _last - _outputHeight);
				if (_scrollBar != null)
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
				if (text == ">" || text == string.Empty)
					_inputTextBox.Text = ">_";
				else if (text == ">_")
					_inputTextBox.Text = ">";
			}
			if (_inputTextBox.IsFocused)
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
			else
			{
				_hintBoxControl.Visible = false;
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
		/// Alignings all UI elements
		/// </summary>
		public void Realign()
		{
			if (_outputPanel is null)
				return;
			float containerHeight = _currentScreenSize.Y * _consoleHeight;
			float inputHeight = _readOnly ? 0f : (_baseInputHeight * _uiScale);
			float scrollBarWidth = _baseScrollWidth * (_uiScale * 0.75f);
			float outputWidth = _currentScreenSize.X - scrollBarWidth;
			int fontSize = _baseFontSize * _uiScale;
			_outputHeight = containerHeight - inputHeight;

			if (_readOnly || _inputTextBox is null)
			{
				_inputTextBox.Visible = false;
			}
			else
			{
				_inputTextBox.Visible = true;
				_inputTextBox.Location = new Vector2(0f, _outputHeight);
				_inputTextBox.Size = new Vector2(_currentScreenSize.X, inputHeight);
				_inputTextBox.Font.Size = fontSize;
				_hintBoxControl.Location = new Vector2(0f, _outputHeight - _hintBoxControl.Height);
			}
			_outputPanel.Location = Vector2.Zero;
			_outputPanel.Size = new Vector2(outputWidth, _outputHeight);
			FontSize = fontSize;
			RealignLogs(true);

			if (_scrollBar is null)
				return;
			_scrollBar.Location = new Vector2(outputWidth, 0f);
			_scrollBar.Size = new Vector2(scrollBarWidth, _outputHeight);
			RealignScrollBar();
		}


		//TODO: Find better way of handling logs (by that I mean not repositioning them after max amount reached)
		/// <summary>
		/// Adds log to console
		/// </summary>
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

		/// <summary>
		/// Clears console from all logs
		/// </summary>
		public void Clear()
		{
			while (_logs.Count > 0)
				_logs.Dequeue().Destroy();
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
		#endregion

		#region Event Handlers
		private void OnTextChanged()
		{
			if (_inputTextBox is null || _hintBox is null || _readOnly || !ShowHints)
				return;
			_hintSelectIndex = -1;
			if (_inputTextBox.Text == ">" || _inputTextBox.Text == ">_" || _inputTextBox.Text.Length < 2)
			{
				_hintList = new List<(string, string)>();
				_hintBoxControl.Visible = false;
				return;
			}
			string text = _inputTextBox.Text.Remove(0, 1);
			IEnumerable<(string, string)> commands = ConsoleManager.Singleton.Commands
				.Where(x => x.Name.StartsWith(text))
				.Select(x => (x.Name, string.Join(" ", x.Parameters
					.Select(y => $"[{y.Name}: {y.ParameterType.Name}]")
				))).OrderBy(x => x.Item1);
			bool refresh = commands.Except(_hintList).Any() || _hintList.Except(commands).Any();
			_hintList = commands;
			if (!refresh)
				return;
			if (!commands.Any())
			{
				_hintBoxControl.Visible = false;
				return;
			}
			_hintBoxControl.DisposeChildren();
			Label longestLabel = null;
			int i = 0;
			foreach ((string, string) command in commands)
			{
				HintLabel label = _hintBoxControl.AddChild<HintLabel>();
				label.Font.Size = _baseFontSize * _uiScale;
				label.AutoWidth = true;
				label.AutoHeight = true;
				label.HorizontalAlignment = TextAlignment.Near;
				label.SetHint(command);
				label.TextColorHighlighted = Color.Yellow;
				label.Clicked += x => {
					_inputTextBox.SetText(">" + x);
					_inputTextBox.SelectionRange = new TextRange(x.Length + 1, x.Length + 1);
				};
				longestLabel ??= label;
				if (label.Text.Value.Length > longestLabel.Text.Value.Length)
					longestLabel = label;
				i++;
				if (i >= _baseMaxHints / _uiScale)
					break;
			}
			Vector2 size = longestLabel.Font.GetFont().MeasureText(longestLabel.Text);
			_hintBoxControl.Width = size.X + 2f;
			_hintBoxControl.Height = i * (longestLabel.Height + _hintBoxControl.Spacing);
			_hintBoxControl.Location = new Vector2(0f, _outputHeight - _hintBoxControl.Height);
			_hintBoxControl.Visible = true;
		}

		private void OnEditEnd()
		{
			if (!Input.GetKeyDown(KeyboardKeys.Return))
				return;
			Debug.Log(_inputTextBox.Text);
			string[] args = _inputTextBox.Text.Remove(0, 1).Trim().Split(' ');
			ConsoleManager.ExecuteCommand(args[0], args.Skip(1).ToArray());
			_inputTextBox.SetText(">");
		}

		private void OnHintChange()
		{
			_hintSelectIndex++;
			if (_hintSelectIndex >= _hintList.Count())
				_hintSelectIndex = 0;
			_hintBoxControl.Children[_hintSelectIndex].OnMouseEnter(Vector2.Zero);
			_hintBoxControl.Children[(_hintSelectIndex == 0 ? _hintList.Count() : _hintSelectIndex) - 1].OnMouseLeave();
		}

		private void OnCommand(string command)
		{
			if(_hintSelectIndex > 0)
			{
				string text = ">" + ((HintLabel)_hintBoxControl.Children[_hintSelectIndex]).HintText;
				_inputTextBox.SetText(text);
				_hintSelectIndex = -1;
				_inputTextBox.SelectionRange = new TextRange(text.Length, text.Length);
				return;
			}
			Debug.Log(_inputTextBox.Text);
			string[] args = command.Split(' ');
			ConsoleManager.ExecuteCommand(args[0], args.Skip(1).ToArray());
			_inputTextBox.SetText(">");
		}
		#endregion
	}
}
