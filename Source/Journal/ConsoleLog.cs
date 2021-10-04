using FlaxEngine;
using FlaxEngine.GUI;

namespace Journal
{
    internal class ConsoleLog
    {
        #region Fields
        public readonly string Text;
        private UIControl _uiElement;
        #endregion

        public Label Label { get; private set; }

        public ConsoleLog(string text)
        {
            Text = text;
        }

        #region Methods
        internal void Spawn(UIControl parent, float width, float y, int fontSize)
        {
            if (_uiElement is object && parent is null)
                return;
            Label = new Label()
            {
                Text = new LocalizedString(Text),
                Width = width,
                AutoHeight = true,
                AutoFitText = false,
                Pivot = new Vector2(0f, 0f),
                BackgroundColor = new Color(0, 0, 0, 40)
            };
            Label.Font.Size = fontSize;
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
        #endregion
    }
}
