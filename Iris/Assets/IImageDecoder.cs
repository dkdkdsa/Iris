using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Assets
{
    public interface IImageDecoder
    {
        public ImageData Decode(ReadOnlySpan<byte> fileBytes);
    }
}
