using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace IrisEditor.Data
{
    internal static class PropertyMerge
    {
        public static void Apply(JsonObject properties, JsonObject defaults)
        {
            if (properties == null || defaults == null)
                return;

            var ordered = new List<KeyValuePair<string, JsonNode>>(properties.Count + defaults.Count);

            foreach (var (key, fallback) in defaults)
            {
                var value = properties.TryGetPropertyValue(key, out var saved) && saved != null
                    ? saved
                    : fallback;

                ordered.Add(new KeyValuePair<string, JsonNode>(key, value?.DeepClone()));
            }

            foreach (var (key, value) in properties)
            {
                if (!defaults.ContainsKey(key))
                    ordered.Add(new KeyValuePair<string, JsonNode>(key, value?.DeepClone()));
            }

            properties.Clear();

            foreach (var (key, value) in ordered)
                properties[key] = value;
        }
    }
}
