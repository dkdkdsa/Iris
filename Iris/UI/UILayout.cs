using Iris.Core;
using Iris.Debugging;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Iris.UI
{
    internal class UILayout : IUILayout
    {
        private readonly List<(string TypeName, JsonObject Properties)> _entries = new();

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

                _entries.Add((typeName, properties));
            }
        }

        public IReadOnlyList<UIObject> Instantiate()
        {
            var result = new List<UIObject>();

            foreach (var (typeName, properties) in _entries)
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
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce($"Failed to create UI object ({typeName})", ex);
                }
            }

            return result;
        }

        public void Dispose()
        {
        }
    }
}
