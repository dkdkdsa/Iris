using Iris.Core;
using Iris.Files;
using Silk.NET.Maths;
using System.IO;
using System.Text.Json.Nodes;

namespace Iris.Assets
{
    internal class SpriteLoader : IAssetLoader
    {
        protected virtual Sprite CreateInstance()
        {
            return new Sprite();
        }

        public IAsset LoadAsset(string path)
        {
            if (IsImageFile(path))
            {
                var imageSprite = CreateInstance();

                if (AtlasManifest.TryResolve(path, out string imagePage, out var imageRegion))
                {
                    imageSprite.Texture = AssetManager.Load<ITexture>(imagePage);
                    imageSprite.SrcRect = imageRegion;
                }
                else
                {
                    imageSprite.Texture = AssetManager.Load<ITexture>(path);
                }

                return imageSprite;
            }

            if (JsonNode.Parse(VirtualFileSystem.ReadAllText(path)) is not JsonObject root)
                throw new InvalidDataException($"Not a sprite file: {path}");

            string texturePath = root["texture"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(texturePath))
                throw new InvalidDataException($"texture is empty: {path}");

            string fullTexturePath = Path.IsPathRooted(texturePath)
                ? texturePath
                : Path.Combine(SceneLoader.ContentRoot, texturePath);

            var sprite = CreateInstance();

            int width = GetInt(root["width"]);
            int height = GetInt(root["height"]);
            int x = GetInt(root["x"]);
            int y = GetInt(root["y"]);

            if (AtlasManifest.TryResolve(fullTexturePath, out string page, out var region))
            {
                sprite.Texture = AssetManager.Load<ITexture>(page);

                sprite.SrcRect = width > 0 && height > 0
                    ? new Rectangle<int>(region.Origin.X + x, region.Origin.Y + y, width, height)
                    : region;

                return sprite;
            }

            sprite.Texture = AssetManager.Load<ITexture>(fullTexturePath);

            if (width > 0 && height > 0)
                sprite.SrcRect = new Rectangle<int>(x, y, width, height);

            return sprite;
        }

        private static bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path);

            return ext.Equals(".png", System.StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".jpg", System.StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".jpeg", System.StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".bmp", System.StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".tga", System.StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".gif", System.StringComparison.OrdinalIgnoreCase);
        }

        private static int GetInt(JsonNode node)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? (int)f : 0;
        }
    }
}
