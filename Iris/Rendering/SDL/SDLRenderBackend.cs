using Iris.Assets;
using Iris.Core;
using Iris.Platform;
using Iris.Platform.SDL;
using Silk.NET.Maths;
using Silk.NET.SDL;

namespace Iris.Rendering.SDL
{
    internal unsafe class SDLRenderBackend : IRenderBackend, ITextureFactory
    {

        private Renderer* _renderer;
        private SDLWindow _window;
        private Sdl _sdl;

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

        public void DrawTexture(ITexture texture, in Rectangle<int> rect, float angle, bool flipX, bool flipY)
        {
            if (texture is SDLTexture tex)
            {
                var flip = RendererFlip.None;
                if (flipX) flip |= RendererFlip.Horizontal;
                if (flipY) flip |= RendererFlip.Vertical;
                _sdl.RenderCopyEx(_renderer, tex.GetNativeTexture(), null, in rect, angle, null, flip);
            }
        }

        public void EndFrame()
        {
            _sdl.RenderPresent(_renderer);
        }

        public ITexture CreateTexture(int width, int height)
        {
            return new SDLTexture(_sdl, _renderer, width, height);
        }

        public void Dispose()
        {
            _sdl.DestroyRenderer(_renderer);
        }
    }
}