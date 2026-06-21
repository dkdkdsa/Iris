using Iris.Core;
using System;

namespace Iris.Rendering
{
    public interface ITexture : IDisposable
    {
        public int Width { get; }
        public int Height { get; }
        public void UpdateTexture(ReadOnlySpan<Color> data);
    }
}
