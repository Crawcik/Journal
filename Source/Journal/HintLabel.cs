using FlaxEngine;
using FlaxEngine.GUI;

namespace Journal
{
	/// <summary>
	/// HintLabel Script.
	/// </summary>
	internal class HintLabel : Label
	{
		private bool _isPressed;
		private string _hintText;

		public string HintText => _hintText;

		public event System.Action<string> Clicked;

		#region Methods
		/// <summary>
		/// Sets hint parameter and text
		/// </summary>
		public void SetHint(Hint hint)
		{
			Text = string.Join(" ", hint.Name, hint.Parameters);
			_hintText = hint.Name;
		}

		/// <summary>
		/// Called when mouse or touch clicks the button.
		/// </summary>
		protected virtual void OnClick()
		{
			Clicked?.Invoke(_hintText);
		}

		/// <summary>
		/// Called when button starts to be pressed by the used (via mouse or touch).
		/// </summary>
		protected virtual void OnPressBegin()
		{
			_isPressed = true;
			if (AutoFocus)
				Focus();
		}

		/// <summary>
		/// Called when button ends to be pressed by the used (via mouse or touch).
		/// </summary>
		protected virtual void OnPressEnd()
		{
			_isPressed = false;
		}

		/// <inheritdoc />
		public override void OnMouseLeave()
		{
			if (_isPressed)
			{
				OnPressEnd();
			}

			base.OnMouseLeave();
		}

		/// <inheritdoc />
		#if FLAX_1_3 || FLAX_1_2 || FLAX_1_1 || FLAX_1_0
		public override bool OnMouseDown(Vector2 location, MouseButton button)
		#else
		public override bool OnMouseDown(Float2 location, MouseButton button)
		#endif
		{
			if (base.OnMouseDown(location, button))
				return true;

			if (button == MouseButton.Left && !_isPressed)
			{
				OnPressBegin();
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		#if FLAX_1_3 || FLAX_1_2 || FLAX_1_1 || FLAX_1_0
		public override bool OnMouseUp(Vector2 location, MouseButton button)
		#else
		public override bool OnMouseUp(Float2 location, MouseButton button)
		#endif
		{
			if (base.OnMouseUp(location, button))
				return true;

			if (button == MouseButton.Left && _isPressed)
			{
				OnPressEnd();
				OnClick();
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		#if FLAX_1_3 || FLAX_1_2 || FLAX_1_1 || FLAX_1_0
		public override bool OnTouchDown(Vector2 location, int pointerId)
		#else
		public override bool OnTouchDown(Float2 location, int pointerId)
		#endif
		{
			if (base.OnTouchDown(location, pointerId))
				return true;

			if (!_isPressed)
			{
				OnPressBegin();
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		#if FLAX_1_3 || FLAX_1_2 || FLAX_1_1 || FLAX_1_0
		public override bool OnTouchUp(Vector2 location, int pointerId)
		#else
		public override bool OnTouchUp(Float2 location, int pointerId)
		#endif
		{
			if (base.OnTouchUp(location, pointerId))
				return true;

			if (_isPressed)
			{
				OnPressEnd();
				OnClick();
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		public override void OnTouchLeave()
		{
			if (_isPressed)
			{
				OnPressEnd();
			}

			base.OnTouchLeave();
		}

		/// <inheritdoc />
		public override void OnLostFocus()
		{
			if (_isPressed)
			{
				OnPressEnd();
			}

			base.OnLostFocus();
		}

		/// <inheritdoc />
		public override void OnSubmit()
		{
			OnClick();

			base.OnSubmit();
		}
		#endregion
	}
}
