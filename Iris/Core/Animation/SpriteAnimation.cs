using Iris.Assets;
using Silk.NET.Maths;
using System;

namespace Iris.Core
{
    public sealed class SpriteAnimation : IAsset
    {
        public ITexture Texture { get; }
        public float Fps { get; set; }
        public bool Loop { get; set; }

        private readonly Rectangle<int>[] _frames;
        public int FrameCount => _frames.Length;

        public SpriteAnimation(ITexture texture, Rectangle<int>[] frames, float fps = 12f, bool loop = true)
        {
            Texture = texture;
            _frames = frames ?? Array.Empty<Rectangle<int>>();
            Fps = fps;
            Loop = loop;
        }

        public Rectangle<int> GetFrame(int index) => _frames[index];

        public void Dispose()
        {
        }

        public static SpriteAnimation FromGrid(
            ITexture texture,
            int frameWidth, int frameHeight, int frameCount,
            float fps = 12f, bool loop = true,
            int offsetX = 0, int offsetY = 0, int columns = 0)
        {
            if (columns <= 0)
                columns = Math.Max(1, (texture.Width - offsetX) / frameWidth);

            var frames = new Rectangle<int>[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                int col = i % columns;
                int row = i / columns;
                frames[i] = new Rectangle<int>(
                    offsetX + col * frameWidth,
                    offsetY + row * frameHeight,
                    frameWidth, frameHeight);
            }

            return new SpriteAnimation(texture, frames, fps, loop);
        }
    }
}
