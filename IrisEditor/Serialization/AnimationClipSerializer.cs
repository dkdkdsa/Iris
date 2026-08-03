using IrisEditor.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IrisEditor.Serialization
{
    internal static class AnimationClipSerializer
    {
        private static readonly JsonSerializerOptions _writeOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static AnimationClipData Load(string path)
        {
            if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject root)
                throw new InvalidDataException($"Not an animation file: {path}");

            var clip = new AnimationClipData
            {
                SampleRate = ReadInt(root["sampleRate"], 12),
                Loop = ReadBool(root["loop"], true),
            };

            if (root["tracks"] is JsonArray tracks)
            {
                foreach (var node in tracks)
                {
                    if (node is JsonObject trackObj)
                        clip.Tracks.Add(ReadTrack(trackObj));
                }

                return clip;
            }

            Promote(root, clip);
            return clip;
        }

        public static void Save(string path, AnimationClipData clip)
        {
            var tracks = new JsonArray();

            foreach (var track in clip.Tracks)
            {
                track.Sort();

                var keys = new JsonArray();

                foreach (var key in track.Keys)
                {
                    keys.Add(new JsonObject
                    {
                        ["t"] = JsonValue.Create(key.Time),
                        ["v"] = key.Value?.DeepClone(),
                    });
                }

                tracks.Add(new JsonObject
                {
                    ["component"] = JsonValue.Create(track.Component),
                    ["property"] = JsonValue.Create(track.Property),
                    ["type"] = JsonValue.Create(KindName(track.Kind)),
                    ["keys"] = keys,
                });
            }

            var root = new JsonObject
            {
                ["sampleRate"] = JsonValue.Create(Math.Max(1, clip.SampleRate)),
                ["loop"] = JsonValue.Create(clip.Loop),
                ["tracks"] = tracks,
            };

            File.WriteAllText(path, root.ToJsonString(_writeOptions));
        }

        public static JsonNode DefaultValue(AnimationTrackKind kind)
        {
            return kind switch
            {
                AnimationTrackKind.Float => JsonValue.Create(0f),
                AnimationTrackKind.Vector2 => new JsonArray(JsonValue.Create(0f), JsonValue.Create(0f)),
                AnimationTrackKind.Color => new JsonArray(
                    JsonValue.Create(255f), JsonValue.Create(255f), JsonValue.Create(255f), JsonValue.Create(255f)),
                _ => JsonValue.Create(string.Empty),
            };
        }

        private static AnimationTrackData ReadTrack(JsonObject obj)
        {
            var track = new AnimationTrackData
            {
                Component = obj["component"]?.GetValue<string>() ?? "Iris.Core.SpriteRenderer",
                Property = obj["property"]?.GetValue<string>() ?? "Sprite",
                Kind = ParseKind(obj["type"]?.GetValue<string>()),
            };

            if (obj["keys"] is JsonArray keys)
            {
                foreach (var node in keys)
                {
                    if (node is not JsonObject key)
                        continue;

                    track.Keys.Add(new AnimationKeyData
                    {
                        Time = ReadFloat(key["t"], 0f),
                        Value = key["v"]?.DeepClone() ?? DefaultValue(track.Kind),
                    });
                }
            }

            track.Sort();
            return track;
        }

        private static void Promote(JsonObject root, AnimationClipData clip)
        {
            string texture = root["texture"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(texture))
                return;

            float fps = ReadFloat(root["fps"], 12f);

            if (fps <= 0f)
                fps = 12f;

            clip.SampleRate = (int)MathF.Round(fps);
            clip.Loop = ReadBool(root["loop"], true);

            var rects = new List<(int X, int Y, int W, int H)>();

            if (root["frames"] is JsonArray frames && frames.Count > 0)
            {
                foreach (var node in frames)
                {
                    if (node is JsonArray { Count: 4 } frame)
                        rects.Add((ReadInt(frame[0], 0), ReadInt(frame[1], 0), ReadInt(frame[2], 0), ReadInt(frame[3], 0)));
                }
            }
            else
            {
                int width = ReadInt(root["frameWidth"], 0);
                int height = ReadInt(root["frameHeight"], 0);

                if (width > 0 && height > 0)
                {
                    int offsetX = ReadInt(root["offsetX"], 0);
                    int offsetY = ReadInt(root["offsetY"], 0);
                    int columns = Math.Max(1, ReadInt(root["columns"], 1));
                    int count = Math.Max(0, ReadInt(root["frameCount"], 0));

                    for (int i = 0; i < count; i++)
                        rects.Add((offsetX + i % columns * width, offsetY + i / columns * height, width, height));
                }
            }

            var track = new AnimationTrackData
            {
                Component = "Iris.Core.SpriteRenderer",
                Property = "Sprite",
                Kind = AnimationTrackKind.Sprite,
            };

            for (int i = 0; i < rects.Count; i++)
            {
                track.Keys.Add(new AnimationKeyData
                {
                    Time = i / fps,
                    Value = new JsonObject
                    {
                        ["texture"] = JsonValue.Create(texture),
                        ["x"] = JsonValue.Create(rects[i].X),
                        ["y"] = JsonValue.Create(rects[i].Y),
                        ["w"] = JsonValue.Create(rects[i].W),
                        ["h"] = JsonValue.Create(rects[i].H),
                    },
                });
            }

            clip.Tracks.Add(track);
        }

        private static AnimationTrackKind ParseKind(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "float" => AnimationTrackKind.Float,
                "vector2" => AnimationTrackKind.Vector2,
                "color" => AnimationTrackKind.Color,
                _ => AnimationTrackKind.Sprite,
            };
        }

        private static string KindName(AnimationTrackKind kind)
        {
            return kind switch
            {
                AnimationTrackKind.Float => "float",
                AnimationTrackKind.Vector2 => "vector2",
                AnimationTrackKind.Color => "color",
                _ => "sprite",
            };
        }

        private static int ReadInt(JsonNode node, int fallback)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? (int)f : fallback;
        }

        private static float ReadFloat(JsonNode node, float fallback)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? f : fallback;
        }

        private static bool ReadBool(JsonNode node, bool fallback)
        {
            return node is JsonValue value && value.TryGetValue(out bool b) ? b : fallback;
        }
    }
}
