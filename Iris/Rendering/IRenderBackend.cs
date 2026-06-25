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
        public void DrawTexture(ITexture texture, in Rectangle<int> rect, float angle, bool flipX, bool flipY);
        public void EndFrame();
    }
}
