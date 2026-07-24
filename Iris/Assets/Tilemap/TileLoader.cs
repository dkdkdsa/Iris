using Iris.Core;

namespace Iris.Assets
{
    internal class TileLoader : SpriteLoader
    {
        protected override Sprite CreateInstance()
        {
            return new Tile();
        }
    }
}
