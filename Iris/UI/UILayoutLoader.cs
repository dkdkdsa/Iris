using Iris.Assets;
using System.IO;

namespace Iris.UI
{
    internal class UILayoutLoader : IAssetLoader
    {
        public IAsset LoadAsset(string path)
        {
            return new UILayout(File.ReadAllText(path));
        }
    }
}
