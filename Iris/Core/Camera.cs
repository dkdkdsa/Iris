using Iris.Rendering;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace Iris.Core
{
    public sealed class Camera : Component
    {
        private static readonly List<Camera> _cameras = new();
        private static Camera _main;

        public static Camera Main
        {
            get => _main ?? (_cameras.Count > 0 ? _cameras[0] : null);
            set => _main = value;
        }

        public static IReadOnlyList<Camera> All => _cameras;

        public float Zoom { get; set; } = 1f;
        public float PixelPerUnit { get; set; } = 100f;
        public Color BackgroundColor { get; set; }

        public Vector2D<int> Viewport { get; private set; }

        public Vector2D<float> Position => Transform.Position;

        protected override void OnAttached()
        {
            _cameras.Add(this);

            var render = SystemManager.Instance?.GetSystem<RenderSystem>();
            if (render != null)
                SetViewport(render.Viewport);
        }

        public override void Dispose()
        {
            _cameras.Remove(this);

            if (_main == this)
                _main = null;
        }

        internal void SetViewport(Vector2D<int> size)
        {
            Viewport = size;
        }

        public Rectangle<int> WorldToScreen(in Rectangle<float> world)
        {
            float centerX = Viewport.X * 0.5f;
            float centerY = Viewport.Y * 0.5f;
            var position = Transform.Position;
            float scale = PixelPerUnit * Zoom;

            float screenX = (world.Origin.X - position.X) * scale + centerX;
            float screenY = (position.Y - world.Origin.Y - world.Size.Y) * scale + centerY;
            var size = world.Size * scale;

            return new Rectangle<int>(
                (int)MathF.Round(screenX), (int)MathF.Round(screenY),
                (int)MathF.Round(size.X), (int)MathF.Round(size.Y));
        }

        public Vector2D<float> ScreenToWorld(in Vector2D<float> screen)
        {
            float centerX = Viewport.X * 0.5f;
            float centerY = Viewport.Y * 0.5f;
            var position = Transform.Position;
            float scale = PixelPerUnit * Zoom;

            float worldX = (screen.X - centerX) / scale + position.X;
            float worldY = position.Y - (screen.Y - centerY) / scale;

            return new Vector2D<float>(worldX, worldY);
        }
    }
}
