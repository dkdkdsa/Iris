using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Assets
{
    internal interface ITextureFactory
    {
        public ITexture CreateTexture(int width, int height);
    }
}
