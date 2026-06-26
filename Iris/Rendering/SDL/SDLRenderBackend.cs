using Iris.Assets;
using Iris.Assets.SDL;
using Iris.Core;
using Iris.Platform;
using Iris.Platform.SDL;
using Silk.NET.Maths;
using Silk.NET.SDL;

namespace Iris.Rendering.SDL
{
    internal unsafe class SDLRenderBackend : IRenderBackend, IFactory<ITexture, Vector2D<int>>
    {
        private readonly Sdl _sdl;
        private Renderer* _renderer;
        private SDLWindow _window;

        public SDLRenderBackend(Sdl sdl)
        {
            _sdl = sdl;
        }
        public void Init(IWindow window)
        {
            _window = window as SDLWindow;
            _renderer = _sdl.CreateRenderer(_window?.GetNativeWindow(), -1, (uint)RendererFlags.Accelerated);
        }

        public void BeginFrame()
        {
            _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 255);
        }

        public void Clear()
        {
            _sdl.RenderClear(_renderer);
        }

        public void DrawTexture(ITexture texture, Rectangle<int>? src, in Rectangle<int> rect, float angle, bool flipX, bool flipY)
        {
            if (texture is SDLTexture tex)
            {
                var flip = RendererFlip.None;
                if (flipX) flip |= RendererFlip.Horizontal;
                if (flipY) flip |= RendererFlip.Vertical;

                if (src.HasValue)
                {
                    var srcRect = src.Value;
                    _sdl.RenderCopyEx(_renderer, tex.GetNativeTexture(), &srcRect, in rect, angle, null, flip);
                }
                else
                {
                    _sdl.RenderCopyEx(_renderer, tex.GetNativeTexture(), null, in rect, angle, null, flip);
                }
            }
        }

        public void EndFrame()
        {
            _sdl.RenderPresent(_renderer);
        }

        public void Dispose()
        {
            _sdl.DestroyRenderer(_renderer);
        }

        public ITexture Create(Vector2D<int> request)
        {
            return new SDLTexture(_sdl, _renderer, request.X, request.Y);
        }
    }
}