using Hexa.NET.ImGui;
using Iris.Assets;
using Iris.Assets.SDL;
using Iris.Core;
using Iris.Debugging;
using Iris.Platform;
using Iris.Platform.SDL;
using Silk.NET.Maths;
using Silk.NET.SDL;
using Color = Silk.NET.SDL.Color;

namespace Iris.Rendering.SDL
{
    internal unsafe class SDLRenderBackend : IRenderBackend, IImGuiRenderer, IFactory<ITexture, Vector2D<int>>
    {
        private static readonly DebugChannel _log = Debug.Channel("Iris");

        private readonly Sdl _sdl;
        private Renderer* _renderer;
        private SDLWindow _window;
        private bool _vsync = true;

        public SDLRenderBackend(Sdl sdl)
        {
            _sdl = sdl;
        }

        public bool VSync
        {
            get
            {
                return _vsync;
            }
            set
            {
                _vsync = value;

                if (_renderer == null)
                    return;

                if (_sdl.RenderSetVSync(_renderer, value ? 1 : 0) != 0)
                    _log.LogWarning($"Failed to toggle vsync ({value}): {_sdl.GetErrorS()}");
            }
        }

        public void Init(IWindow window)
        {
            _window = window as SDLWindow;

            var flags = RendererFlags.Accelerated;

            if (_vsync)
                flags |= RendererFlags.Presentvsync;

            _renderer = _sdl.CreateRenderer(_window?.GetNativeWindow(), -1, (uint)flags);

            if (_renderer == null)
            {
                _log.LogError($"Failed to create renderer: {_sdl.GetErrorS()}");
                return;
            }

            if (_vsync && !HasVSync())
                _log.LogWarning("Could not enable vsync; use targetFrameRate to limit frames.");
        }

        private bool HasVSync()
        {
            RendererInfo info = default;

            if (_sdl.GetRendererInfo(_renderer, &info) != 0)
                return false;

            return (info.Flags & (uint)RendererFlags.Presentvsync) != 0;
        }

        public void BeginFrame()
        {
            _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 255);
        }

        public void Clear(Iris.Core.Color clearColor)
        {
            _sdl.SetRenderDrawColor(_renderer, clearColor.r, clearColor.g, clearColor.b, 255);
            _sdl.RenderClear(_renderer);
        }

        public void DrawTexture(ITexture texture, Rectangle<int>? src, in Rectangle<int> rect, float angle, bool flipX, bool flipY, Iris.Core.Color? tint = null)
        {
            if (texture is SDLTexture tex)
            {
                var flip = RendererFlip.None;
                if (flipX) flip |= RendererFlip.Horizontal;
                if (flipY) flip |= RendererFlip.Vertical;

                var native = tex.GetNativeTexture();
                var mod = tint ?? new Iris.Core.Color(255, 255, 255, 255);
                _sdl.SetTextureColorMod(native, mod.r, mod.g, mod.b);
                _sdl.SetTextureAlphaMod(native, mod.a);

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

        public IRenderTarget CreateRenderTarget(Vector2D<int> size)
        {
            return new SDLRenderTarget(_sdl, _renderer, size.X, size.Y);
        }

        public void SetRenderTarget(IRenderTarget target)
        {
            _sdl.SetRenderTarget(_renderer, target is SDLRenderTarget sdlTarget ? sdlTarget.GetNativeTexture() : null);
        }

        public void Dispose()
        {
            _sdl.DestroyRenderer(_renderer);
        }

        public ITexture Create(Vector2D<int> request)
        {
            return new SDLTexture(_sdl, _renderer, request.X, request.Y);
        }


        public void RenderDrawData(ImDrawDataPtr dd)
        {
            if (dd.Textures.Data != null)
                for (int i = 0; i < dd.Textures.Size; i++)
                    UpdateTexture(dd.Textures[i]);

            var off = dd.DisplayPos;
            for (int n = 0; n < dd.CmdListsCount; n++)
            {
                var cl = dd.CmdLists[n];
                var vtx = (ImDrawVert*)cl.VtxBuffer.Data;
                var idx = (ushort*)cl.IdxBuffer.Data;

                for (int c = 0; c < cl.CmdBuffer.Size; c++)
                {
                    var cmd = cl.CmdBuffer[c];
                    var clip = new Rectangle<int>(
                        (int)(cmd.ClipRect.X - off.X), (int)(cmd.ClipRect.Y - off.Y),
                        (int)(cmd.ClipRect.Z - cmd.ClipRect.X), (int)(cmd.ClipRect.W - cmd.ClipRect.Y));
                    _sdl.RenderSetClipRect(_renderer, &clip);

                    byte* @base = (byte*)(vtx + cmd.VtxOffset);
                    _sdl.RenderGeometryRaw(_renderer, (Texture*)(nint)cmd.GetTexID(),
                        (float*)(@base + 0), sizeof(ImDrawVert),
                        (Color*)(@base + 16), sizeof(ImDrawVert),
                        (float*)(@base + 8), sizeof(ImDrawVert),
                        (int)(cl.VtxBuffer.Size - cmd.VtxOffset),
                        idx + cmd.IdxOffset, (int)cmd.ElemCount, sizeof(ushort));
                }
            }
        }

        private void UpdateTexture(ImTextureDataPtr tex)
        {
            if (tex.Status == ImTextureStatus.WantCreate)
            {
                var t = _sdl.CreateTexture(_renderer, (uint)PixelFormatEnum.Rgba32,
                                           (int)TextureAccess.Static, tex.Width, tex.Height);
                _sdl.SetTextureBlendMode(t, BlendMode.Blend);
                _sdl.SetTextureScaleMode(t, ScaleMode.Linear);
                _sdl.UpdateTexture(t, null, tex.GetPixels(), tex.GetPitch());
                tex.SetTexID((nint)t);
                tex.SetStatus(ImTextureStatus.Ok);
            }
            else if (tex.Status == ImTextureStatus.WantUpdates)
            {
                var t = (Texture*)(nint)tex.TexID;
                for (int i = 0; i < tex.Updates.Size; i++)
                {
                    var r = tex.Updates[i];
                    var rect = new Rectangle<int>(r.X, r.Y, r.W, r.H);
                    _sdl.UpdateTexture(t, &rect, tex.GetPixelsAt(r.X, r.Y), tex.GetPitch());
                }
                tex.SetStatus(ImTextureStatus.Ok);
            }
            else if (tex.Status == ImTextureStatus.WantDestroy)
            {
                _sdl.DestroyTexture((Texture*)(nint)tex.TexID);
                tex.SetTexID(ImTextureID.Null);
                tex.SetStatus(ImTextureStatus.Destroyed);
            }
        }

    }
}