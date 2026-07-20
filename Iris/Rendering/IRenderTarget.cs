using System;

namespace Iris.Rendering
{
    public interface IRenderTarget : IDisposable
    {
        public int Width { get; }
        public int Height { get; }

        public nint Handle { get; }
    }
}
