using Iris.Assets;
using Iris.Files;
using System.IO;

namespace Iris.UI
{
    internal class UILayoutLoader : IAssetLoader
    {
        public IAsset LoadAsset(string path)
        {
            return new UILayout(VirtualFileSystem.ReadAllText(path));
        }
    }
}
