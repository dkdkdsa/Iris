using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Assets
{
    internal abstract class AssetAPI
    {
        public static AssetAPI ActiveAPI { get; set; }

        internal abstract void Init();
        internal abstract T LoadAsset<T>(string path) where T : IAsset; 
    }
}