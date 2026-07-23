using Hexa.NET.ImGui;
using IrisEditor.Workspace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;

namespace IrisEditor.Panels
{
    internal static class PropertyDrawer
    {
        public static bool Draw(JsonObject properties, IReadOnlyDictionary<string, Type> assetProps,
                                EditorWorkspace workspace, IReadOnlySet<string> hidden = null)
        {
            bool changed = false;
            bool drewAny = false;

            if (properties != null)
            {
                foreach (var (key, value) in properties.ToList())
                {
                    if (hidden != null && hidden.Contains(key))
                        continue;

                    drewAny = true;

                    var updated = assetProps != null && assetProps.TryGetValue(key, out var assetType)
                        ? DrawAssetProperty(key, value, assetType, workspace)
                        : DrawProperty(key, value);

                    if (updated != null)
                    {
                        properties[key] = updated;
                        changed = true;
                    }
                }
            }

            if (!drewAny)
                ImGui.TextDisabled("(데이터 없음)");

            return changed;
        }

        private static JsonNode DrawAssetProperty(string label, JsonNode value, Type assetType, EditorWorkspace workspace)
        {
            string current = value is JsonValue v && v.TryGetValue(out string s) ? s : string.Empty;

            return AssetPicker.Draw(label, current, assetType, workspace);
        }

        private static JsonNode DrawProperty(string label, JsonNode value)
        {
            switch (value)
            {
                case JsonValue v when v.TryGetValue(out bool b):
                    if (ImGui.Checkbox(label, ref b))
                        return JsonValue.Create(b);
                    return null;

                case JsonValue v when v.TryGetValue(out float f):
                    if (ImGui.DragFloat(label, ref f, 0.1f))
                        return JsonValue.Create(f);
                    return null;

                case JsonValue v when v.TryGetValue(out string s):
                    if (ImGui.InputText(label, ref s, 256))
                        return JsonValue.Create(s);
                    return null;

                case JsonArray { Count: 2 } arr when TryGetVector2(arr, out var vec):
                    if (ImGui.DragFloat2(label, ref vec, 0.1f))
                        return new JsonArray(vec.X, vec.Y);
                    return null;

                case JsonArray { Count: 4 } arr when TryGetColor(arr, out var color):
                    if (ImGui.ColorEdit4(label, ref color))
                        return ToColorArray(color);
                    return null;

                default:
                    ImGui.TextDisabled($"{label}: {value?.ToJsonString() ?? "null"}");
                    return null;
            }
        }

        private static bool TryGetVector2(JsonArray arr, out Vector2 vec)
        {
            vec = default;

            if (arr[0] is JsonValue a && a.TryGetValue(out float x) &&
                arr[1] is JsonValue b && b.TryGetValue(out float y))
            {
                vec = new Vector2(x, y);
                return true;
            }

            return false;
        }

        private static bool TryGetColor(JsonArray arr, out Vector4 color)
        {
            color = default;

            if (arr[0] is JsonValue r && r.TryGetValue(out float x) &&
                arr[1] is JsonValue g && g.TryGetValue(out float y) &&
                arr[2] is JsonValue b && b.TryGetValue(out float z) &&
                arr[3] is JsonValue a && a.TryGetValue(out float w))
            {
                color = new Vector4(x, y, z, w) / 255f;
                return true;
            }

            return false;
        }

        private static JsonArray ToColorArray(Vector4 color)
        {
            var bytes = Vector4.Clamp(color, Vector4.Zero, Vector4.One) * 255f;

            return new JsonArray(
                MathF.Round(bytes.X), MathF.Round(bytes.Y),
                MathF.Round(bytes.Z), MathF.Round(bytes.W));
        }
    }
}
