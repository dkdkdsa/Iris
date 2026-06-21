using System;

namespace Iris.Rendering
{
    internal unsafe interface IRenderObject : IDisposable
    {
        public int Order { get; }

        public void Init(IRenderBackend backend);
        public void Render(IRenderBackend backend);
    }
}
