using Iris;
using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IrisEditor.Serialization
{
    internal static class ProjectConfigSerializer
    {
        public const string FileName = "project.json";

        private static readonly JsonSerializerOptions _writeOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static ProjectConfig Load(string path, out JsonObject raw)
        {
            var config = new ProjectConfig();
            raw = new JsonObject();

            if (!File.Exists(path))
                return config;

            if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject obj)
                return config;

            raw = obj;

            config.StartScene = ReadString(obj, "startScene") ?? config.StartScene;
            config.Title = ReadString(obj, "title") ?? config.Title;
            config.DefaultWidth = ReadInt(obj, "width") ?? config.DefaultWidth;
            config.DefaultHeight = ReadInt(obj, "height") ?? config.DefaultHeight;
            config.Fullscreen = ReadBool(obj, "fullscreen") ?? config.Fullscreen;
            config.Resizable = ReadBool(obj, "resizable") ?? config.Resizable;

            return config;
        }

        public static void Save(string path, ProjectConfig config, JsonObject raw)
        {
            var obj = raw?.DeepClone() as JsonObject ?? new JsonObject();

            obj["startScene"] = string.IsNullOrWhiteSpace(config.StartScene)
                ? null
                : JsonValue.Create(config.StartScene.Replace('\\', '/'));

            obj["width"] = JsonValue.Create(Math.Max(1, config.DefaultWidth));
            obj["height"] = JsonValue.Create(Math.Max(1, config.DefaultHeight));
            obj["title"] = JsonValue.Create(config.Title ?? string.Empty);
            obj["fullscreen"] = JsonValue.Create(config.Fullscreen);
            obj["resizable"] = JsonValue.Create(config.Resizable);

            File.WriteAllText(path, obj.ToJsonString(_writeOptions));
        }

        private static string ReadString(JsonObject obj, string key)
        {
            try
            {
                return obj[key] is JsonValue v && v.TryGetValue(out string s) ? s : null;
            }
            catch
            {
                return null;
            }
        }

        private static int? ReadInt(JsonObject obj, string key)
        {
            try
            {
                return obj[key] is JsonValue v && v.TryGetValue(out float f) ? (int)f : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool? ReadBool(JsonObject obj, string key)
        {
            try
            {
                return obj[key] is JsonValue v && v.TryGetValue(out bool b) ? b : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
