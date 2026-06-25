using Silk.NET.Maths;
using System;

namespace Iris.Core
{
    //컴포넌트로 바꾸기
    public sealed class Camera
    {
        public Vector2D<float> Position { get; set; } = Vector2D<float>.Zero;
        public float Zoom { get; set; } = 1f;

        public float PixelPerUnit { get; set; } = 100f;

        private Vector2D<float> _viewport;

        public Camera(int viewportWidth, int viewportHeight)
            => Resize(viewportWidth, viewportHeight);

        public void Resize(int width, int height)
            => _viewport = new Vector2D<float>(width, height);

        public Rectangle<int> WorldToScreen(in Rectangle<float> world)
        {
            var center = _viewport * 0.5f;
            float scale = PixelPerUnit * Zoom;

            float screenX = (world.Origin.X - Position.X) * scale + center.X;
            float screenY = (Position.Y - world.Origin.Y - world.Size.Y) * scale + center.Y;
            var size = world.Size * scale;

            return new Rectangle<int>(
                (int)MathF.Round(screenX), (int)MathF.Round(screenY),
                (int)MathF.Round(size.X), (int)MathF.Round(size.Y));
        }
    }
}