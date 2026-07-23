using System.IO;

namespace Iris.Assets
{
    internal class StbFontLoader : IAssetLoader
    {
        public IAsset LoadAsset(string path)
        {
            return new StbFont(File.ReadAllBytes(path));
        }
    }
}
