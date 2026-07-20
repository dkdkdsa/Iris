using Hexa.NET.ImGui;
using IrisEditor.Rendering;
using System;
using System.Numerics;

namespace IrisEditor.Panels
{
    internal sealed class CameraPanel : EditorPanel
    {
        private static readonly (string Label, float Width, float Height)[] _resolutions =
        {
            ("800 × 600", 800f, 600f),
            ("1024 × 768", 1024f, 768f),
            ("1280 × 720", 1280f, 720f),
            ("1600 × 900", 1600f, 900f),
            ("1920 × 1080", 1920f, 1080f),
        };

        private readonly SceneRenderer _renderer;
        private int _resolutionIndex;

        public CameraPanel(SceneRenderer renderer)
        {
            _renderer = renderer;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                if (_resolutions[i].Width == renderer.ReferenceWidth &&
                    _resolutions[i].Height == renderer.ReferenceHeight)
                {
                    _resolutionIndex = i;
                    break;
                }
            }
        }

        public override string Title => "카메라";

        protected override void OnGui()
        {
            DrawResolutionCombo();

            var draw = ImGui.GetWindowDrawList();
            var origin = ImGui.GetCursorScreenPos();
            var size = ImGui.GetContentRegionAvail();

            if (size.X < 1f || size.Y < 1f)
                return;

            draw.AddRectFilled(origin, origin + size, 0xFF101010);

            float refWidth = _renderer.ReferenceWidth;
            float refHeight = _renderer.ReferenceHeight;

            float fit = MathF.Min(size.X / refWidth, size.Y / refHeight);
            var screenSize = new Vector2(refWidth, refHeight) * fit;
            var screenMin = origin + (size - screenSize) * 0.5f;
            var screenMax = screenMin + screenSize;

            draw.AddRectFilled(screenMin, screenMax, 0xFF000000);
            draw.AddRect(screenMin - Vector2.One, screenMax + Vector2.One, 0xFF404040);

            if (!_renderer.TryGetCamera(out var camPos, out float scale))
            {
                draw.AddText(screenMin + new Vector2(8f, 8f), 0xFF888888, "카메라 없음 - 씬에 Camera 컴포넌트를 추가하세요");
                return;
            }

            draw.PushClipRect(screenMin, screenMax, true);
            _renderer.DrawSprites(draw, camPos, scale * fit, (screenMin + screenMax) * 0.5f, showSelection: false);
            draw.PopClipRect();
        }

        private void DrawResolutionCombo()
        {
            ImGui.SetNextItemWidth(140f);

            if (ImGui.BeginCombo("##Resolution", _resolutions[_resolutionIndex].Label))
            {
                for (int i = 0; i < _resolutions.Length; i++)
                {
                    if (ImGui.Selectable(_resolutions[i].Label, i == _resolutionIndex))
                    {
                        _resolutionIndex = i;
                        _renderer.ReferenceWidth = _resolutions[i].Width;
                        _renderer.ReferenceHeight = _resolutions[i].Height;
                    }
                }

                ImGui.EndCombo();
            }
        }
    }
}
