using Iris.Assets;
using Iris.Core;
using Iris.Platform;
using Silk.NET.Maths;
using System;

namespace Iris.Rendering
{
    public interface IRenderBackend : IDisposable
    {
        public bool VSync { get; set; }
        public void Init(IWindow window);
        public void BeginFrame();
        public void Clear(Color clearColor);
        public void DrawTexture(ITexture texture, Rectangle<int>? src, in Rectangle<int> rect, float angle, bool flipX, bool flipY, Color? tint = null);
        public void EndFrame();
        public IRenderTarget CreateRenderTarget(Vector2D<int> size);
        public void SetRenderTarget(IRenderTarget target);
    }
}
