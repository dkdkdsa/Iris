using Iris.Core;
using System;

namespace Iris.Assets
{
    public interface ITexture : IAsset
    {
        public int Width { get; }
        public int Height { get; }

        public nint Handle { get; }

        public void UpdateTexture(ReadOnlySpan<Color> data);
    }
}
