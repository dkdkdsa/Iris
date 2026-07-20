using System;
using System.Numerics;
using System.Text.Json.Nodes;

namespace IrisEditor.Data
{
    internal class ComponentData
    {
        public Guid Id { get; set; }
        public Type TargetType { get; set; }
        public string TypeName { get; set; }
        public JsonNode Properties { get; set; }

        public float GetFloat(string key, float fallback)
        {
            if (Properties is JsonObject obj && obj[key] is JsonValue v && v.TryGetValue(out float f))
                return f;

            return fallback;
        }

        public string GetString(string key, string fallback)
        {
            if (Properties is JsonObject obj && obj[key] is JsonValue v && v.TryGetValue(out string s))
                return s;

            return fallback;
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
            if (Properties is JsonObject obj && obj[key] is JsonArray { Count: 2 } arr &&
                arr[0] is JsonValue a && a.TryGetValue(out float x) &&
                arr[1] is JsonValue b && b.TryGetValue(out float y))
                return new Vector2(x, y);

            return fallback;
        }
    }
}
