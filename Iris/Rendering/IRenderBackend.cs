using Iris.Platform;
using Silk.NET.Maths;
using System;

namespace Iris.Rendering
{
    internal interface IRenderBackend : IDisposable
    {
        public void Init(IWindow window);
        public ITexture CreateTexture(int width, int height);
        public void BeginFrame();
        public void Clear();
        public void DrawTexture(ITexture texture, in Rectangle<int> rect);
        public void EndFrame();
    }
}
