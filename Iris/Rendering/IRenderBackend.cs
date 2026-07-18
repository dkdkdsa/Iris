using Iris.Assets;
using Iris.Platform;
using Silk.NET.Maths;
using System;

namespace Iris.Rendering
{
    public interface IRenderBackend : IDisposable
    {
        public void Init(IWindow window);
        public void BeginFrame();
        public void Clear();
        public void DrawTexture(ITexture texture, Rectangle<int>? src, in Rectangle<int> rect, float angle, bool flipX, bool flipY);
        public void EndFrame();

        public IRenderTarget CreateRenderTarget(Vector2D<int> size);

        /// <summary>이후 그리기가 들어갈 대상. null이면 백버퍼로 되돌린다.</summary>
        public void SetRenderTarget(IRenderTarget target);
    }
}
