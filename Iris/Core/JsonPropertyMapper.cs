using Iris.Assets;
using Iris.Debugging;
using Silk.NET.Maths;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Iris.Core
{
    public static class JsonPropertyMapper
    {
        private static readonly DebugChannel _log = Debug.Channel("Iris");

        public static void ApplyProperties(object target, JsonObject properties)
        {
            if (properties == null)
                return;

            var type = target.GetType();

            foreach (var pair in properties)
            {
                try
                {
                    var prop = type.GetProperty(pair.Key, BindingFlags.Public | BindingFlags.Instance);

                    if (prop != null && prop.GetSetMethod() != null)
                    {
                        if (TryFromJson(prop.PropertyType, pair.Value, out var value))
                            prop.SetValue(target, value);

                        continue;
                    }

                    var field = type.GetField(pair.Key, BindingFlags.Public | BindingFlags.Instance);

                    if (field == null || field.IsInitOnly)
                        continue;

                    if (TryFromJson(field.FieldType, pair.Value, out var fieldValue))
                        field.SetValue(target, fieldValue);
                }
                catch (Exception ex)
                {
                    _log.LogExceptionOnce($"Failed to apply member ({type.Name}.{pair.Key})", ex);
                }
            }
        }

        public static bool TryFromJson(Type targetType, JsonNode node, out object value)
        {
            value = null;

            if (node == null)
                return false;

            if (typeof(IAsset).IsAssignableFrom(targetType))
            {
                string assetPath = node is JsonValue av && av.TryGetValue(out string ap) ? ap : null;

                if (string.IsNullOrWhiteSpace(assetPath))
                    return true;

                value = LoadAsset(targetType, assetPath);
                return true;
            }

            if (targetType == typeof(string))
            {
                if (node is JsonValue sv && sv.TryGetValue(out string s))
                {
                    value = s;
                    return true;
                }

                return false;
            }

            if (targetType.IsEnum)
            {
                if (node is JsonValue ev && ev.TryGetValue(out string name) &&
                    Enum.TryParse(targetType, name, true, out var parsed))
                {
                    value = parsed;
                    return true;
                }

                return false;
            }

            if (targetType == typeof(bool))
            {
                if (node is JsonValue bv && bv.TryGetValue(out bool b))
                {
                    value = b;
                    return true;
                }

                return false;
            }

            if (targetType == typeof(Vector2D<float>))
            {
                if (node is JsonArray { Count: 2 } arr)
                {
                    value = new Vector2D<float>(GetFloat(arr[0]), GetFloat(arr[1]));
                    return true;
                }

                return false;
            }

            if (targetType == typeof(Vector2D<int>))
            {
                if (node is JsonArray { Count: 2 } arr)
                {
                    value = new Vector2D<int>((int)GetFloat(arr[0]), (int)GetFloat(arr[1]));
                    return true;
                }

                return false;
            }

            if (targetType == typeof(Color))
            {
                if (node is JsonArray { Count: 4 } arr)
                {
                    value = new Color((byte)GetFloat(arr[0]), (byte)GetFloat(arr[1]), (byte)GetFloat(arr[2]), (byte)GetFloat(arr[3]));
                    return true;
                }

                return false;
            }

            if (targetType.IsPrimitive)
            {
                if (node is JsonValue nv && nv.TryGetValue(out float f))
                {
                    value = Convert.ChangeType(f, targetType);
                    return true;
                }

                return false;
            }

            return false;
        }

        private static float GetFloat(JsonNode node)
        {
            return node is JsonValue v && v.TryGetValue(out float f) ? f : 0f;
        }

        private static object LoadAsset(Type assetType, string relativePath)
        {
            string fullPath = Path.IsPathRooted(relativePath)
                ? relativePath
                : Path.Combine(SceneLoader.ContentRoot, relativePath);

            try
            {
                return typeof(AssetManager).GetMethod(nameof(AssetManager.Load))
                    .MakeGenericMethod(assetType)
                    .Invoke(null, new object[] { fullPath });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }
    }
}
