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
                imageSprite.Texture = AssetManager.Load<ITexture>(path);
                return imageSprite;
            }

            if (JsonNode.Parse(VirtualFileSystem.ReadAllText(path)) is not JsonObject root)
                throw new InvalidDataException($"스프라이트 파일 형식이 아닙니다: {path}");

            string texturePath = root["texture"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(texturePath))
                throw new InvalidDataException($"texture가 비어 있습니다: {path}");

            string fullTexturePath = Path.IsPathRooted(texturePath)
                ? texturePath
                : Path.Combine(SceneLoader.ContentRoot, texturePath);

            var sprite = CreateInstance();
            sprite.Texture = AssetManager.Load<ITexture>(fullTexturePath);

            int width = GetInt(root["width"]);
            int height = GetInt(root["height"]);

            if (width > 0 && height > 0)
                sprite.SrcRect = new Rectangle<int>(GetInt(root["x"]), GetInt(root["y"]), width, height);

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
