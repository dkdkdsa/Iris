using Iris.Assets;
using Silk.NET.Maths;
using Silk.NET.SDL;
using System;
using Color = Iris.Core.Color;

namespace Iris.Assets.SDL
{
    internal unsafe class SDLTexture : ITexture
    {
        private Texture* _texture;
        private Sdl _sdl;

        public int Width { get; init; }
        public int Height { get; init; }

        public SDLTexture(Sdl sdl, Renderer* renderer, int width, int height)
        {
            _sdl = sdl;
            Width = width;
            Height = height;
            _texture = _sdl.CreateTexture(renderer,
                Sdl.PixelformatAbgr8888,
                (int)TextureAccess.Streaming, width, height);
        }

        public void UpdateTexture(ReadOnlySpan<Color> data)
        {
            _sdl.UpdateTexture(_texture, (Rectangle<int>*)null, data, Width * sizeof(Color));
        }

        public Texture* GetNativeTexture()
        {
            return _texture;
        }

        public void Dispose()
        {
            _sdl.DestroyTexture(_texture);
        }
    }
}
