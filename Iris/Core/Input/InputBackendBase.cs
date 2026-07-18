using Silk.NET.Maths;
using System.Collections.Generic;

namespace Iris.Core
{
    public abstract class InputBackendBase : IInputBackend
    {
        private readonly HashSet<KeyCode> _currentKeys = new();
        private readonly HashSet<KeyCode> _downKeys = new();
        private readonly HashSet<KeyCode> _upKeys = new();

        private readonly HashSet<MouseButton> _currentMouseButtons = new();
        private readonly HashSet<MouseButton> _downMouseButtons = new();
        private readonly HashSet<MouseButton> _upMouseButtons = new();

        private readonly List<char> _textInput = new();

        public IReadOnlyList<char> TextInput => _textInput;

        public Vector2D<float> MousePosition { get; private set; }
        public Vector2D<int> MouseDelta { get; private set; }
        public Vector2D<int> MouseScrollDelta { get; private set; }

        public bool AnyKey => _currentKeys.Count > 0;
        public bool AnyKeyDown => _downKeys.Count > 0;

        public bool GetKey(KeyCode key)
        {
            return _currentKeys.Contains(key);
        }

        public bool GetKeyDown(KeyCode key)
        {
            return _downKeys.Contains(key);
        }

        public bool GetKeyUp(KeyCode key)
        {
            return _upKeys.Contains(key);
        }

        public bool GetMouseButton(MouseButton button)
        {
            return _currentMouseButtons.Contains(button);
        }

        public bool GetMouseButtonDown(MouseButton button)
        {
            return _downMouseButtons.Contains(button);
        }

        public bool GetMouseButtonUp(MouseButton button)
        {
            return _upMouseButtons.Contains(button);
        }

        public virtual void BeginFrame()
        {
            _downKeys.Clear();
            _upKeys.Clear();

            _downMouseButtons.Clear();
            _upMouseButtons.Clear();

            _textInput.Clear();

            MouseDelta = Vector2D<int>.Zero;
            MouseScrollDelta = Vector2D<int>.Zero;
        }

        protected void NotifyText(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                _textInput.Add(text[i]);
            }
        }

        protected void NotifyKeyDown(KeyCode key, bool repeat = false)
        {
            if (repeat || key == KeyCode.None)
                return;

            if (_currentKeys.Add(key))
            {
                _downKeys.Add(key);
            }
        }

        protected void NotifyKeyUp(KeyCode key)
        {
            if (key == KeyCode.None)
                return;

            if (_currentKeys.Remove(key))
            {
                _upKeys.Add(key);
            }
        }

        protected void NotifyMouseButtonDown(MouseButton button, int x, int y)
        {
            MousePosition = new Vector2D<float>(x, y);

            if (_currentMouseButtons.Add(button))
            {
                _downMouseButtons.Add(button);
            }
        }

        protected void NotifyMouseButtonUp(MouseButton button, int x, int y)
        {
            MousePosition = new Vector2D<float>(x, y);

            if (_currentMouseButtons.Remove(button))
            {
                _upMouseButtons.Add(button);
            }
        }

        protected void NotifyMouseMove(int x, int y, int deltaX, int deltaY)
        {
            MousePosition = new Vector2D<float>(x, y);
            MouseDelta += new Vector2D<int>(deltaX, deltaY);
        }

        protected void NotifyMouseWheel(int x, int y)
        {
            MouseScrollDelta += new Vector2D<int>(x, y);
        }
    }
}
