using Iris.Assets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExampleGame
{
    public static class Helper
    {
        public static T GetAsset<T>(string path) where T : IAsset
        {
            return AssetManager.Load<T>(Path.Combine(AppContext.BaseDirectory, path));
        }
    }
}
