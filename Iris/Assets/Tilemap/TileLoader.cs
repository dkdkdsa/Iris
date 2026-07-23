using Iris.Core;
using Silk.NET.Maths;
using System.IO;
using System.Text.Json.Nodes;

namespace Iris.Assets
{
    internal class TileLoader : IAssetLoader
    {
        public IAsset LoadAsset(string path)
        {
            if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject root)
                throw new InvalidDataException($"타일 파일 형식이 아닙니다: {path}");

            string texturePath = root["texture"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(texturePath))
                throw new InvalidDataException($"texture가 비어 있습니다: {path}");

            string fullTexturePath = Path.IsPathRooted(texturePath)
                ? texturePath
                : Path.Combine(SceneLoader.ContentRoot, texturePath);

            var tile = new Tile
            {
                Texture = AssetManager.Load<ITexture>(fullTexturePath),
            };

            int width = GetInt(root["width"]);
            int height = GetInt(root["height"]);

            if (width > 0 && height > 0)
                tile.SrcRect = new Rectangle<int>(GetInt(root["x"]), GetInt(root["y"]), width, height);

            return tile;
        }

        private static int GetInt(JsonNode node)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? (int)f : 0;
        }
    }
}
