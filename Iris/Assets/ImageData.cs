using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Assets
{
    public ref struct ImageData
    {
        public readonly int width;
        public readonly int height;
        public readonly ReadOnlySpan<byte> pixels;
        public ImageData(int width, int height, ReadOnlySpan<byte> pixels)
        {
            this.width = width;
            this.height = height;
            this.pixels = pixels; 
        }
    }
}
