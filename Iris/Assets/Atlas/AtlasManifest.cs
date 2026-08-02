using Iris.Core;
using Iris.Debugging;
using Iris.Files;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace Iris.Assets
{
    public static class AtlasManifest
    {
        public const string FileName = "atlas.json";

        private static readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
        private static readonly List<string> _pages = new();

        private static bool _loaded;

        public static int EntryCount => _entries.Count;

        public static int PageCount => _pages.Count;

        public static void Reset()
        {
            _entries.Clear();
            _pages.Clear();
            _loaded = false;
        }

        public static bool TryResolve(string texturePath, out string pagePath, out Rectangle<int> region)
        {
            pagePath = null;
            region = default;

            EnsureLoaded();

            if (_entries.Count == 0)
                return false;

            if (!_entries.TryGetValue(ToKey(texturePath), out var entry))
                return false;

            pagePath = Path.Combine(SceneLoader.ContentRoot, _pages[entry.Page]);
            region = entry.Region;

            return true;
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;

            string path = Path.Combine(SceneLoader.ContentRoot, FileName);

            if (!VirtualFileSystem.Exists(path))
                return;

            try
            {
                if (JsonNode.Parse(VirtualFileSystem.ReadAllText(path)) is not JsonObject root)
                    return;

                if (root["pages"] is JsonArray pages)
                {
                    foreach (var node in pages)
                    {
                        if (node is JsonValue value && value.TryGetValue(out string page))
                            _pages.Add(page);
                    }
                }

                if (root["entries"] is not JsonObject entries)
                    return;

                foreach (var pair in entries)
                {
                    if (pair.Value is not JsonObject item)
                        continue;

                    int page = GetInt(item["page"]);

                    if (page < 0 || page >= _pages.Count)
                        continue;

                    _entries[ToKey(pair.Key)] = new Entry(page, new Rectangle<int>(
                        GetInt(item["x"]), GetInt(item["y"]), GetInt(item["w"]), GetInt(item["h"])));
                }

                Debug.Log($"Atlas: {_entries.Count} entries across {_pages.Count} page(s)");
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to read the atlas manifest", ex);

                _entries.Clear();
                _pages.Clear();
            }
        }

        private static string ToKey(string path)
        {
            string relative = Path.IsPathRooted(path)
                ? Path.GetRelativePath(SceneLoader.ContentRoot, path)
                : path;

            return relative.Replace('\\', '/').ToLowerInvariant();
        }

        private static int GetInt(JsonNode node)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? (int)f : 0;
        }

        private readonly struct Entry
        {
            public readonly int Page;
            public readonly Rectangle<int> Region;

            public Entry(int page, Rectangle<int> region)
            {
                Page = page;
                Region = region;
            }
        }
    }
}
