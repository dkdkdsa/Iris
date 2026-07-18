using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace Iris.Core
{
    public static class Input
    {
        private static IInputBackend _backend;

        public static Vector2D<float> MousePosition => _backend?.MousePosition ?? default;
        public static Vector2D<int> MouseDelta => _backend?.MouseDelta ?? default;
        public static Vector2D<int> MouseScrollDelta => _backend?.MouseScrollDelta ?? default;

        /// <summary>이번 프레임에 입력된 문자. 키 상태가 아니라 IME까지 거친 최종 텍스트다.</summary>
        public static IReadOnlyList<char> TextInput => _backend?.TextInput ?? Array.Empty<char>();

        public static bool AnyKey => _backend?.AnyKey ?? false;
        public static bool AnyKeyDown => _backend?.AnyKeyDown ?? false;

        public static bool GetKey(KeyCode key)
        {
            return _backend?.GetKey(key) ?? false;
        }

        public static bool GetKeyDown(KeyCode key)
        {
            return _backend?.GetKeyDown(key) ?? false;
        }

        public static bool GetKeyUp(KeyCode key)
        {
            return _backend?.GetKeyUp(key) ?? false;
        }

        public static bool GetMouseButton(MouseButton button)
        {
            return _backend?.GetMouseButton(button) ?? false;
        }

        public static bool GetMouseButtonDown(MouseButton button)
        {
            return _backend?.GetMouseButtonDown(button) ?? false;
        }

        public static bool GetMouseButtonUp(MouseButton button)
        {
            return _backend?.GetMouseButtonUp(button) ?? false;
        }

        internal static void SetBackend(IInputBackend backend)
        {
            _backend = backend;
        }

        internal static void BeginFrame()
        {
            _backend?.BeginFrame();
        }
    }
}
