using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Assets
{
    public interface IAssetLoader
    {
        public IAsset LoadAsset(string path);
    }
}
