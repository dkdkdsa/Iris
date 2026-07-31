using Iris.Debugging;
using IrisEditor.Localization;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IrisEditor
{
    internal static class EditorSettings
    {
        public const string FileName = "editor.json";

        private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

        public static string Language { get; set; } = Loc.DefaultLanguage;

        public static int LayoutVersion { get; set; }

        private static string Path => System.IO.Path.Combine(AppContext.BaseDirectory, FileName);

        public static void Load()
        {
            string path = Path;

            if (!File.Exists(path))
                return;

            try
            {
                if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject root)
                    return;

                if (root["language"] is JsonValue value && value.TryGetValue(out string language))
                    Language = language;

                if (root["layoutVersion"] is JsonValue version && version.TryGetValue(out int layoutVersion))
                    LayoutVersion = layoutVersion;
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to read editor settings", ex);
            }
        }

        public static void Save()
        {
            try
            {
                var root = new JsonObject
                {
                    ["language"] = JsonValue.Create(Language),
                    ["layoutVersion"] = JsonValue.Create(LayoutVersion),
                };

                File.WriteAllText(Path, root.ToJsonString(_writeOptions));
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to write editor settings", ex);
            }
        }
    }
}
