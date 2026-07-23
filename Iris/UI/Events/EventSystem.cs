using Hexa.NET.ImGui;
using Iris.Core;
using Silk.NET.Maths;
using System;

namespace Iris.UI.Events
{
    public class EventSystem : SystemBase
    {
        public static bool PointerOverUI { get; private set; }

        private UIObject _hover;
        private Canvas _hoverCanvas;
        private UIObject _pressed;

        public EventSystem()
        {
            Order = -100;
        }

        public override void Update()
        {
            _hover = null;
            _hoverCanvas = null;

            if (!ImGui.GetCurrentContext().IsNull && ImGui.GetIO().WantCaptureMouse)
            {
                PointerOverUI = true;
                _pressed = null;
                return;
            }

            var mouse = Input.MousePosition;

            for (int i = Canvas.All.Count - 1; i >= 0; i--)
            {
                var hit = Canvas.All[i].HitTest(mouse);

                if (hit != null)
                {
                    _hover = hit;
                    _hoverCanvas = Canvas.All[i];
                    break;
                }
            }

            PointerOverUI = _hover != null;

            if (Input.GetMouseButtonDown(MouseButton.Left))
            {
                _pressed = _hover;

                if (_hover is IPointerDown down)
                    down.OnPointerDown(CreateArgs(mouse));
            }

            if (Input.GetMouseButtonUp(MouseButton.Left))
            {
                if (_hover is IPointerUp up)
                    up.OnPointerUp(CreateArgs(mouse));

                if (_pressed != null && _pressed == _hover && _hover is IPointerClick click)
                    click.OnPointerClick(CreateArgs(mouse));

                _pressed = null;
            }
        }

        private PointerEventArgs CreateArgs(Vector2D<float> mouse)
        {
            var args = new PointerEventArgs
            {
                Target = _hover,
                Canvas = _hoverCanvas,
                ScreenPosition = mouse,
                Button = MouseButton.Left,
            };

            if (_hover != null && _hoverCanvas != null)
            {
                var rect = _hoverCanvas.CalculateRect(_hover);

                if (rect.Size.X > 0f && rect.Size.Y > 0f)
                {
                    args.LocalPosition = new Vector2D<float>(
                        (mouse.X - rect.Origin.X) / rect.Size.X,
                        (mouse.Y - rect.Origin.Y) / rect.Size.Y);
                }
            }

            return args;
        }
    }
}
