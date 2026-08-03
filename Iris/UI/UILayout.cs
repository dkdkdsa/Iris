using Iris.Core;
using Iris.Debugging;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Iris.UI
{
    internal class UILayout : IUILayout
    {
        private readonly List<(string TypeName, string Id, string ParentId, JsonObject Properties)> _entries = new();

        public UILayout(string json)
        {
            if (JsonNode.Parse(json) is not JsonObject root)
                throw new InvalidOperationException("Not a UI layout file.");

            if (root["uiObjects"] is not JsonArray uiObjects)
                return;

            foreach (var node in uiObjects)
            {
                if (node is not JsonObject obj)
                    continue;

                string typeName = obj["type"]?.GetValue<string>();

                if (typeName == null)
                    continue;

                var properties = obj["properties"]?.DeepClone() as JsonObject;

                if (properties != null && properties["Sprite"] == null && properties["Texture"] is JsonNode textureNode)
                {
                    properties.Remove("Texture");
                    properties["Sprite"] = textureNode;
                }

                _entries.Add((typeName, obj["id"]?.GetValue<string>(), obj["parent"]?.GetValue<string>(), properties));
            }
        }

        public IReadOnlyList<UIObject> Instantiate()
        {
            var result = new List<UIObject>();
            var byId = new Dictionary<string, UIObject>(StringComparer.OrdinalIgnoreCase);
            var links = new List<(UIObject Child, string ParentId)>();

            foreach (var (typeName, id, parentId, properties) in _entries)
            {
                var type = RuntimeTypeResolver.ResolveUIObject(typeName);

                if (type == null)
                {
                    Debug.LogOnce(LogLevel.Warning, $"Skipping unknown UI object type: {typeName}");
                    continue;
                }

                try
                {
                    var uiObject = (UIObject)Activator.CreateInstance(type);
                    JsonPropertyMapper.ApplyProperties(uiObject, properties);
                    result.Add(uiObject);

                    if (!string.IsNullOrEmpty(id))
                        byId[id] = uiObject;

                    if (!string.IsNullOrEmpty(parentId))
                        links.Add((uiObject, parentId));
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce($"Failed to create UI object ({typeName})", ex);
                }
            }

            foreach (var (child, parentId) in links)
            {
                if (byId.TryGetValue(parentId, out var parent))
                    child.SetParent(parent);
                else
                    Debug.LogOnce(LogLevel.Warning, $"UI parent not found; keeping at root: {child.Name}");
            }

            return result;
        }

        public void Dispose()
        {
        }
    }
}
