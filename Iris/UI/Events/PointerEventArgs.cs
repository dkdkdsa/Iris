using Iris.Core;
using Silk.NET.Maths;

namespace Iris.UI.Events
{
    public class PointerEventArgs
    {
        public UIObject Target { get; set; }
        public Canvas Canvas { get; set; }
        public Vector2D<float> ScreenPosition { get; set; }
        public Vector2D<float> LocalPosition { get; set; }
        public MouseButton Button { get; set; }
        public bool Handled { get; set; }
    }
}
