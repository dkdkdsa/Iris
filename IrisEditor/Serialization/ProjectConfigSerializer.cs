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

            config.Title = ReadString(obj, "title") ?? config.Title;
            config.DefaultWidth = ReadInt(obj, "width") ?? config.DefaultWidth;
            config.DefaultHeight = ReadInt(obj, "height") ?? config.DefaultHeight;
            config.Fullscreen = ReadBool(obj, "fullscreen") ?? config.Fullscreen;
            config.Resizable = ReadBool(obj, "resizable") ?? config.Resizable;
            config.VSync = ReadBool(obj, "vsync") ?? config.VSync;
            config.TargetFrameRate = ReadInt(obj, "targetFrameRate") ?? config.TargetFrameRate;
            config.LogToFile = ReadBool(obj, "logToFile") ?? config.LogToFile;

            if (obj["buildScenes"] is JsonArray buildScenes)
            {
                foreach (var node in buildScenes)
                {
                    if (node is JsonValue v && v.TryGetValue(out string scene) && !string.IsNullOrWhiteSpace(scene))
                        config.BuildScenes.Add(scene.Replace('\\', '/'));
                }
            }

            if (config.BuildScenes.Count == 0 && ReadString(obj, "startScene") is string legacy &&
                !string.IsNullOrWhiteSpace(legacy))
                config.BuildScenes.Add(legacy.Replace('\\', '/'));

            return config;
        }

        public static void Save(string path, ProjectConfig config, JsonObject raw)
        {
            var obj = raw?.DeepClone() as JsonObject ?? new JsonObject();

            obj.Remove("startScene");

            var buildScenes = new JsonArray();

            foreach (var scene in config.BuildScenes)
            {
                if (!string.IsNullOrWhiteSpace(scene))
                    buildScenes.Add(JsonValue.Create(scene.Replace('\\', '/')));
            }

            obj["buildScenes"] = buildScenes;

            obj["width"] = JsonValue.Create(Math.Max(1, config.DefaultWidth));
            obj["height"] = JsonValue.Create(Math.Max(1, config.DefaultHeight));
            obj["title"] = JsonValue.Create(config.Title ?? string.Empty);
            obj["fullscreen"] = JsonValue.Create(config.Fullscreen);
            obj["resizable"] = JsonValue.Create(config.Resizable);
            obj["vsync"] = JsonValue.Create(config.VSync);
            obj["targetFrameRate"] = JsonValue.Create(Math.Max(0, config.TargetFrameRate));
            obj["logToFile"] = JsonValue.Create(config.LogToFile);

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
