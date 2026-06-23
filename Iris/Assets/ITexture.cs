using Iris.Core;
using System;

namespace Iris.Assets
{
    public interface ITexture : IDisposable
    {
        public int Width { get; }
        public int Height { get; }
        public void UpdateTexture(ReadOnlySpan<Color> data);
    }
}
