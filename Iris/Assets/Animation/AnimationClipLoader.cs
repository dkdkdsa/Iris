using Iris.Core;
using Iris.Debugging;
using Iris.Files;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace Iris.Assets
{
    internal class AnimationClipLoader : IAssetLoader
    {
        private const string DefaultComponent = "Iris.Core.SpriteRenderer";
        private const string DefaultProperty = "Sprite";

        public IAsset LoadAsset(string path)
        {
            if (JsonNode.Parse(VirtualFileSystem.ReadAllText(path)) is not JsonObject root)
                throw new InvalidDataException($"Not an animation file: {path}");

            string name = Path.GetFileNameWithoutExtension(path);
            int sampleRate = GetInt(root["sampleRate"], 12);
            bool loop = GetBool(root["loop"], true);
            float length = GetFloat(root["length"], 0f);

            if (root["tracks"] is JsonArray trackArray)
            {
                var tracks = new List<AnimationTrack>();

                foreach (var node in trackArray)
                {
                    if (node is not JsonObject trackObj)
                        continue;

                    var track = ReadTrack(trackObj);

                    if (track != null)
                        tracks.Add(track);
                }

                return new AnimationClip(name, tracks.ToArray(), sampleRate, loop, length);
            }

            return Promote(root, name, path, loop);
        }

        private static AnimationTrack ReadTrack(JsonObject obj)
        {
            string component = obj["component"]?.GetValue<string>() ?? DefaultComponent;
            string property = obj["property"]?.GetValue<string>() ?? DefaultProperty;
            string kind = obj["type"]?.GetValue<string>()?.ToLowerInvariant() ?? "sprite";

            if (obj["keys"] is not JsonArray keys)
                return null;

            switch (kind)
            {
                case "sprite":
                    return new SpriteTrack(component, property, ReadKeys(keys, ReadSprite));

                case "float":
                    return new FloatTrack(component, property, ReadKeys(keys, node => GetFloat(node, 0f)));

                case "vector2":
                    return new Vector2Track(component, property, ReadKeys(keys, ReadVector2));

                case "color":
                    return new ColorTrack(component, property, ReadKeys(keys, ReadColor));

                default:
                    Debug.LogOnce(LogLevel.Warning, $"Unknown animation track type: {kind}");
                    return null;
            }
        }

        private static Keyframe<T>[] ReadKeys<T>(JsonArray keys, Func<JsonNode, T> read)
        {
            var result = new List<Keyframe<T>>(keys.Count);

            foreach (var node in keys)
            {
                if (node is not JsonObject key)
                    continue;

                result.Add(new Keyframe<T>(GetFloat(key["t"], 0f), read(key["v"])));
            }

            result.Sort(static (a, b) => a.Time.CompareTo(b.Time));

            return result.ToArray();
        }

        private static Sprite ReadSprite(JsonNode node)
        {
            if (node is JsonObject inline)
                return ReadInlineSprite(inline);

            if (node is not JsonValue value || !value.TryGetValue(out string relative) || string.IsNullOrWhiteSpace(relative))
                return null;

            return AssetManager.Load<Sprite>(Resolve(relative));
        }

        private static Sprite ReadInlineSprite(JsonObject node)
        {
            string texturePath = node["texture"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(texturePath))
                return null;

            string full = Resolve(texturePath);

            int x = GetInt(node["x"], 0);
            int y = GetInt(node["y"], 0);
            int width = GetInt(node["w"], 0);
            int height = GetInt(node["h"], 0);

            ITexture texture;

            if (AtlasManifest.TryResolve(full, out string page, out var region))
            {
                texture = AssetManager.Load<ITexture>(page);
                x += region.Origin.X;
                y += region.Origin.Y;
            }
            else
            {
                texture = AssetManager.Load<ITexture>(full);
            }

            if (texture == null)
                return null;

            return new Sprite
            {
                Texture = texture,
                SrcRect = width > 0 && height > 0 ? new Rectangle<int>(x, y, width, height) : null,
            };
        }

        private static string Resolve(string relative)
        {
            return Path.IsPathRooted(relative) ? relative : Path.Combine(SceneLoader.ContentRoot, relative);
        }

        private static Vector2D<float> ReadVector2(JsonNode node)
        {
            if (node is JsonArray { Count: 2 } array)
                return new Vector2D<float>(GetFloat(array[0], 0f), GetFloat(array[1], 0f));

            return default;
        }

        private static Color ReadColor(JsonNode node)
        {
            if (node is JsonArray { Count: 4 } array)
                return new Color(
                    (byte)GetFloat(array[0], 255f), (byte)GetFloat(array[1], 255f),
                    (byte)GetFloat(array[2], 255f), (byte)GetFloat(array[3], 255f));

            return new Color(255, 255, 255, 255);
        }

        private static AnimationClip Promote(JsonObject root, string name, string path, bool loop)
        {
            string texturePath = root["texture"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(texturePath))
                throw new InvalidDataException($"Animation has neither tracks nor a texture: {path}");

            string fullTexturePath = Path.IsPathRooted(texturePath)
                ? texturePath
                : Path.Combine(SceneLoader.ContentRoot, texturePath);

            ITexture texture;
            int sheetWidth;
            int sheetHeight;
            int originX = 0;
            int originY = 0;

            if (AtlasManifest.TryResolve(fullTexturePath, out string page, out var region))
            {
                texture = AssetManager.Load<ITexture>(page);

                originX = region.Origin.X;
                originY = region.Origin.Y;
                sheetWidth = region.Size.X;
                sheetHeight = region.Size.Y;
            }
            else
            {
                texture = AssetManager.Load<ITexture>(fullTexturePath);

                sheetWidth = texture?.Width ?? 0;
                sheetHeight = texture?.Height ?? 0;
            }

            float fps = GetFloat(root["fps"], 12f);

            if (fps <= 0f)
                fps = 12f;

            var rects = new List<Rectangle<int>>();

            if (root["frames"] is JsonArray framesArray && framesArray.Count > 0)
            {
                foreach (var node in framesArray)
                {
                    if (node is JsonArray { Count: 4 } frame)
                        rects.Add(new Rectangle<int>(
                            originX + GetInt(frame[0], 0), originY + GetInt(frame[1], 0),
                            GetInt(frame[2], 0), GetInt(frame[3], 0)));
                }
            }
            else
            {
                int frameWidth = GetInt(root["frameWidth"], 0);
                int frameHeight = GetInt(root["frameHeight"], 0);

                if (frameWidth > 0 && frameHeight > 0)
                {
                    int offsetX = GetInt(root["offsetX"], 0);
                    int offsetY = GetInt(root["offsetY"], 0);
                    int columns = GetInt(root["columns"], 0);
                    int frameCount = GetInt(root["frameCount"], 0);

                    if (columns <= 0)
                        columns = Math.Max(1, (sheetWidth - offsetX) / frameWidth);

                    if (frameCount <= 0)
                        frameCount = columns * Math.Max(1, (sheetHeight - offsetY) / frameHeight);

                    for (int i = 0; i < frameCount; i++)
                        rects.Add(new Rectangle<int>(
                            originX + offsetX + i % columns * frameWidth,
                            originY + offsetY + i / columns * frameHeight,
                            frameWidth, frameHeight));
                }
            }

            var keys = new Keyframe<Sprite>[rects.Count];

            for (int i = 0; i < rects.Count; i++)
                keys[i] = new Keyframe<Sprite>(i / fps, new Sprite { Texture = texture, SrcRect = rects[i] });

            var track = new SpriteTrack(DefaultComponent, DefaultProperty, keys);

            return new AnimationClip(name, new AnimationTrack[] { track },
                (int)MathF.Round(fps), loop, rects.Count / fps);
        }

        private static int GetInt(JsonNode node, int fallback)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? (int)f : fallback;
        }

        private static float GetFloat(JsonNode node, float fallback)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? f : fallback;
        }

        private static bool GetBool(JsonNode node, bool fallback)
        {
            return node is JsonValue value && value.TryGetValue(out bool b) ? b : fallback;
        }
    }
}
