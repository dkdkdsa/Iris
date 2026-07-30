using Iris.Core;
using Iris.Files;
using System.IO;
using System.Text.Json.Nodes;

namespace Iris.Assets
{
    internal class PrefabLoader : IAssetLoader
    {
        public IAsset LoadAsset(string path)
        {
            if (JsonNode.Parse(VirtualFileSystem.ReadAllText(path)) is not JsonObject root ||
                root["actors"] is not JsonArray actors)
                throw new InvalidDataException($"프리팹 파일 형식이 아닙니다: {path}");

            return new Prefab(actors.DeepClone().AsArray());
        }
    }
}
