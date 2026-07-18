using Silk.NET.SDL;

namespace Iris.Rendering.SDL
{
    internal unsafe sealed class SDLRenderTarget : IRenderTarget
    {
        private readonly Sdl _sdl;
        private Texture* _texture;

        public int Width { get; }
        public int Height { get; }
        public nint Handle => (nint)_texture;

        public SDLRenderTarget(Sdl sdl, Renderer* renderer, int width, int height)
        {
            _sdl = sdl;
            Width = width;
            Height = height;

            _texture = _sdl.CreateTexture(renderer, Sdl.PixelformatAbgr8888,
                                          (int)TextureAccess.Target, width, height);
            _sdl.SetTextureBlendMode(_texture, BlendMode.Blend);
        }

        internal Texture* GetNativeTexture() => _texture;

        public void Dispose()
        {
            if (_texture == null)
                return;

            _sdl.DestroyTexture(_texture);
            _texture = null;
        }
    }
}
