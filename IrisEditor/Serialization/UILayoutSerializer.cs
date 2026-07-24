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
                throw new InvalidDataException("UI 레이아웃 파일 형식이 아닙니다.");

            if (root["uiObjects"] is not JsonArray uiObjects)
                return entries;

            foreach (var node in uiObjects)
            {
                if (node is not JsonObject obj)
                    continue;

                string typeName = obj["type"]?.GetValue<string>();
                var type = UIObjectCatalog.Resolve(typeName);

                if (type == null)
                    Console.WriteLine($"[에디터] 알 수 없는 UI 오브젝트 타입(데이터는 보존됨): {typeName}");

                var properties = obj["properties"]?.DeepClone() as JsonObject ?? new JsonObject();

                if (properties["Sprite"] == null && properties["Texture"] is JsonNode textureNode)
                {
                    properties.Remove("Texture");
                    properties["Sprite"] = textureNode;
                }

                UIObjectCatalog.ApplyMissingDefaults(properties, type);

                entries.Add(new ComponentData
                {
                    Id = Guid.TryParse(obj["id"]?.GetValue<string>(), out var id) ? id : Guid.NewGuid(),
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

                uiObjects.Add(new JsonObject
                {
                    ["id"] = entry.Id.ToString(),
                    ["type"] = typeName,
                    ["properties"] = entry.Properties?.DeepClone() ?? new JsonObject(),
                });
            }

            var root = new JsonObject
            {
                ["uiObjects"] = uiObjects,
            };

            File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
