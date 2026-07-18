using Silk.NET.Maths;
using System.Collections.Generic;

namespace Iris.Core
{
    public interface IInputBackend
    {
        public Vector2D<float> MousePosition { get; }
        public Vector2D<int> MouseDelta { get; }
        public Vector2D<int> MouseScrollDelta { get; }

        /// <summary>이번 프레임에 입력된 문자. 키 상태가 아니라 IME까지 거친 최종 텍스트다.</summary>
        public IReadOnlyList<char> TextInput { get; }

        public bool AnyKey { get; }
        public bool AnyKeyDown { get; }

        public bool GetKey(KeyCode key);
        public bool GetKeyDown(KeyCode key);
        public bool GetKeyUp(KeyCode key);

        public bool GetMouseButton(MouseButton button);
        public bool GetMouseButtonDown(MouseButton button);
        public bool GetMouseButtonUp(MouseButton button);

        public void BeginFrame();
    }
}
