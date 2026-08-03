using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Nodes;

namespace IrisEditor.Data
{
    internal class ComponentData
    {
        public Guid Id { get; set; }
        public Type TargetType { get; set; }
        public string TypeName { get; set; }
        public bool Enabled { get; set; } = true;
        public Guid? ParentId { get; set; }
        public JsonNode Properties { get; set; }

        private Dictionary<string, JsonNode> _preview;

        public bool HasPreview => _preview != null && _preview.Count > 0;

        public void SetPreview(string key, JsonNode value)
        {
            _preview ??= new Dictionary<string, JsonNode>(StringComparer.Ordinal);
            _preview[key] = value;
        }

        public void ClearPreview()
        {
            _preview = null;
        }

        public JsonNode GetPreview(string key)
        {
            return _preview != null && _preview.TryGetValue(key, out var value) ? value : null;
        }

        private JsonNode Read(string key)
        {
            var preview = GetPreview(key);

            if (preview != null)
                return preview;

            return Properties is JsonObject obj ? obj[key] : null;
        }

        public float GetFloat(string key, float fallback)
        {
            if (Read(key) is JsonValue v && v.TryGetValue(out float f))
                return f;

            return fallback;
        }

        public bool GetBool(string key, bool fallback)
        {
            if (Read(key) is JsonValue v && v.TryGetValue(out bool b))
                return b;

            return fallback;
        }

        public string GetString(string key, string fallback)
        {
            if (Read(key) is JsonValue v && v.TryGetValue(out string s))
                return s;

            return fallback;
        }

        public void SetFloat(string key, float value)
        {
            if (Properties is JsonObject obj)
                obj[key] = JsonValue.Create(value);
        }

        public void SetString(string key, string value)
        {
            if (Properties is JsonObject obj)
                obj[key] = JsonValue.Create(value ?? string.Empty);
        }

        public void SetVector2(string key, Vector2 value)
        {
            if (Properties is JsonObject obj)
                obj[key] = new JsonArray(value.X, value.Y);
        }

        public Vector2 GetVector2(string key, Vector2 fallback)
        {
            if (Read(key) is JsonArray { Count: 2 } arr &&
                arr[0] is JsonValue a && a.TryGetValue(out float x) &&
                arr[1] is JsonValue b && b.TryGetValue(out float y))
                return new Vector2(x, y);

            return fallback;
        }
    }
}
