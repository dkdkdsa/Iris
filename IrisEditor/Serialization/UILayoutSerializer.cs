using Iris.Debugging;
using IrisEditor.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IrisEditor.Serialization
{
    internal static class UILayoutSerializer
    {
        public static List<ComponentData> Load(string path)
        {
            var entries = new List<ComponentData>();

            if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject root)
                throw new InvalidDataException("Not a UI layout file.");

            if (root["uiObjects"] is not JsonArray uiObjects)
                return entries;

            foreach (var node in uiObjects)
            {
                if (node is not JsonObject obj)
                    continue;

                string typeName = obj["type"]?.GetValue<string>();
                var type = UIObjectCatalog.Resolve(typeName);

                if (type == null)
                    Debug.LogOnce(LogLevel.Warning, $"Unknown UI object type (data preserved): {typeName}");

                var properties = obj["properties"]?.DeepClone() as JsonObject ?? new JsonObject();

                if (properties["Sprite"] == null && properties["Texture"] is JsonNode textureNode)
                {
                    properties.Remove("Texture");
                    properties["Sprite"] = textureNode;
                }

                if (properties["AnchorMax"] == null)
                    properties["AnchorMax"] = properties["Anchor"]?.DeepClone() ?? new JsonArray(0f, 0f);

                UIObjectCatalog.ApplyMissingDefaults(properties, type);

                entries.Add(new ComponentData
                {
                    Id = Guid.TryParse(obj["id"]?.GetValue<string>(), out var id) ? id : Guid.NewGuid(),
                    ParentId = Guid.TryParse(obj["parent"]?.GetValue<string>(), out var parent) ? parent : null,
                    TargetType = type,
                    TypeName = typeName,
                    Properties = properties,
                });
            }

            return entries;
        }

        public static void Save(List<ComponentData> entries, string path)
        {
            var uiObjects = new JsonArray();

            foreach (var entry in entries)
            {
                string typeName = entry.TargetType?.FullName ?? entry.TypeName;

                if (typeName == null)
                    continue;

                var obj = new JsonObject
                {
                    ["id"] = entry.Id.ToString(),
                    ["type"] = typeName,
                };

                if (entry.ParentId is Guid parentId)
                    obj["parent"] = parentId.ToString();

                obj["properties"] = entry.Properties?.DeepClone() ?? new JsonObject();

                uiObjects.Add(obj);
            }

            var root = new JsonObject
            {
                ["uiObjects"] = uiObjects,
            };

            File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
