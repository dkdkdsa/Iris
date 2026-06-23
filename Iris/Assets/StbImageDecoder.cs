using StbiSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Assets
{
    internal class StbImageDecoder : IImageDecoder
    {
        public ImageData Decode(ReadOnlySpan<byte> fileBytes)
        {

            var image = Stbi.LoadFromMemory(fileBytes, 4);

            return new ImageData(image.Width, image.Height, image.Data);
        }
    }
}
