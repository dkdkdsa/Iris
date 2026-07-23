using Iris.Assets;
using Silk.NET.Maths;

namespace Iris.Core
{
    public class Tile : IAsset
    {
        public ITexture Texture { get; set; }
        public Rectangle<int>? SrcRect { get; set; }

        public void Dispose()
        {
        }
    }
}
