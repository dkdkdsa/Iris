using Silk.NET.Maths;
using Silk.NET.SDL;
using System.Collections.Generic;

namespace Iris.Core
{
    public static class Input
    {
        private static readonly HashSet<KeyCode> _currentKeys = new();
        private static readonly HashSet<KeyCode> _downKeys = new();
        private static readonly HashSet<KeyCode> _upKeys = new();

        private static readonly HashSet<byte> _currentMouseButtons = new();
        private static readonly HashSet<byte> _downMouseButtons = new();
        private static readonly HashSet<byte> _upMouseButtons = new();

        public static Vector2D<int> MousePosition { get; private set; }
        public static Vector2D<int> MouseDelta { get; private set; }
        public static Vector2D<int> MouseScrollDelta { get; private set; }

        public static bool AnyKey => _currentKeys.Count > 0;
        public static bool AnyKeyDown => _downKeys.Count > 0;


        public static bool GetKey(KeyCode key)
        {
            return _currentKeys.Contains(key);
        }

        public static bool GetKeyDown(KeyCode key)
        {
            return _downKeys.Contains(key);
        }

        public static bool GetKeyUp(KeyCode key)
        {
            return _upKeys.Contains(key);
        }

        public static bool GetMouseButton(int button)
        {
            return _currentMouseButtons.Contains((byte)button);
        }

        public static bool GetMouseButtonDown(int button)
        {
            return _downMouseButtons.Contains((byte)button);
        }

        public static bool GetMouseButtonUp(int button)
        {
            return _upMouseButtons.Contains((byte)button);
        }

        internal static void BeginFrame()
        {
            _downKeys.Clear();
            _upKeys.Clear();

            _downMouseButtons.Clear();
            _upMouseButtons.Clear();

            MouseDelta = Vector2D<int>.Zero;
            MouseScrollDelta = Vector2D<int>.Zero;
        }

        internal static void NotifyKeyDown(KeyCode key, bool repeat = false)
        {
            if (repeat)
                return;

            if (_currentKeys.Add(key))
            {
                _downKeys.Add(key);
            }
        }

        internal static void NotifyKeyUp(KeyCode key)
        {
            if (_currentKeys.Remove(key))
            {
                _upKeys.Add(key);
            }
        }

        internal static void NotifyMouseButtonDown(byte button, int x, int y)
        {
            MousePosition = new Vector2D<int>(x, y);

            if (_currentMouseButtons.Add(button))
            {
                _downMouseButtons.Add(button);
            }
        }

        internal static void NotifyMouseButtonUp(byte button, int x, int y)
        {
            MousePosition = new Vector2D<int>(x, y);

            if (_currentMouseButtons.Remove(button))
            {
                _upMouseButtons.Add(button);
            }
        }

        internal static void NotifyMouseMove(int x, int y, int deltaX, int deltaY)
        {
            MousePosition = new Vector2D<int>(x, y);
            MouseDelta += new Vector2D<int>(deltaX, deltaY);
        }

        internal static void NotifyMouseWheel(int x, int y)
        {
            MouseScrollDelta += new Vector2D<int>(x, y);
        }
    }
}