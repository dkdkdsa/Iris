using Iris.Core;
using Iris.Files;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace Iris.Assets
{
    internal class SpriteAnimationLoader : IAssetLoader
    {
        public IAsset LoadAsset(string path)
        {
            if (JsonNode.Parse(VirtualFileSystem.ReadAllText(path)) is not JsonObject root)
                throw new InvalidDataException($"스프라이트 애니메이션 파일 형식이 아닙니다: {path}");

            string texturePath = root["texture"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(texturePath))
                throw new InvalidDataException($"texture가 비어 있습니다: {path}");

            string fullTexturePath = Path.IsPathRooted(texturePath)
                ? texturePath
                : Path.Combine(SceneLoader.ContentRoot, texturePath);

            var texture = AssetManager.Load<ITexture>(fullTexturePath);

            float fps = GetFloat(root["fps"], 12f);
            bool loop = root["loop"] is JsonValue loopValue && loopValue.TryGetValue(out bool l) ? l : true;

            if (root["frames"] is JsonArray framesArray && framesArray.Count > 0)
            {
                var frames = new List<Rectangle<int>>();

                foreach (var node in framesArray)
                {
                    if (node is JsonArray { Count: 4 } frame)
                        frames.Add(new Rectangle<int>(
                            GetInt(frame[0], 0), GetInt(frame[1], 0),
                            GetInt(frame[2], 0), GetInt(frame[3], 0)));
                }

                return new SpriteAnimation(texture, frames.ToArray(), fps, loop);
            }

            int frameWidth = GetInt(root["frameWidth"], 0);
            int frameHeight = GetInt(root["frameHeight"], 0);

            if (frameWidth <= 0 || frameHeight <= 0)
                return new SpriteAnimation(texture, Array.Empty<Rectangle<int>>(), fps, loop);

            int offsetX = GetInt(root["offsetX"], 0);
            int offsetY = GetInt(root["offsetY"], 0);
            int columns = GetInt(root["columns"], 0);
            int frameCount = GetInt(root["frameCount"], 0);

            if (columns <= 0)
                columns = Math.Max(1, (texture.Width - offsetX) / frameWidth);

            if (frameCount <= 0)
            {
                int rows = Math.Max(1, (texture.Height - offsetY) / frameHeight);
                frameCount = columns * rows;
            }

            return SpriteAnimation.FromGrid(texture, frameWidth, frameHeight, frameCount, fps, loop, offsetX, offsetY, columns);
        }

        private static float GetFloat(JsonNode node, float fallback)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? f : fallback;
        }

        private static int GetInt(JsonNode node, int fallback)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? (int)f : fallback;
        }
    }
}
