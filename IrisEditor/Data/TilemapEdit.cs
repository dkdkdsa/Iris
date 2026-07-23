using Iris.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Nodes;

namespace IrisEditor.Data
{
    internal static class TilemapEdit
    {
        private sealed class Cache
        {
            public string Json;
            public List<string> Palette;
            public Dictionary<(int X, int Y), int> Cells;
        }

        private static readonly Dictionary<Guid, Cache> _cache = new();

        public static IReadOnlyDictionary<(int X, int Y), int> GetCells(ComponentData tilemap, out IReadOnlyList<string> palette)
        {
            var cache = GetCache(tilemap);
            palette = cache.Palette;
            return cache.Cells;
        }

        public static bool Paint(ComponentData tilemap, (int X, int Y) cell, string tilePath)
        {
            if (string.IsNullOrEmpty(tilePath))
                return false;

            var cache = GetCache(tilemap);
            int index = cache.Palette.IndexOf(tilePath);

            if (index < 0)
            {
                cache.Palette.Add(tilePath);
                index = cache.Palette.Count - 1;
            }

            if (cache.Cells.TryGetValue(cell, out int existing) && existing == index)
                return false;

            cache.Cells[cell] = index;
            WriteBack(tilemap, cache);
            return true;
        }

        public static bool Erase(ComponentData tilemap, (int X, int Y) cell)
        {
            var cache = GetCache(tilemap);

            if (!cache.Cells.Remove(cell))
                return false;

            WriteBack(tilemap, cache);
            return true;
        }

        public static (Vector2 Origin, Vector2 CellSize) FindGrid(SceneData scene, ActorData actor)
        {
            var current = actor;

            for (int i = 0; i < 64 && current != null; i++)
            {
                var grid = current.GetComponent(typeof(Grid));

                if (grid != null)
                    return (SceneTransforms.GetWorld(scene, current).Position, grid.GetVector2("CellSize", Vector2.One));

                current = SceneTransforms.FindParent(scene, current);
            }

            return (SceneTransforms.GetWorld(scene, actor).Position, Vector2.One);
        }

        public static (int X, int Y) WorldToCell(Vector2 world, Vector2 origin, Vector2 cellSize)
        {
            float sizeX = cellSize.X > 0f ? cellSize.X : 1f;
            float sizeY = cellSize.Y > 0f ? cellSize.Y : 1f;

            return (
                (int)MathF.Floor((world.X - origin.X) / sizeX),
                (int)MathF.Floor((world.Y - origin.Y) / sizeY));
        }

        private static Cache GetCache(ComponentData tilemap)
        {
            string json = tilemap.GetString("TilesJson", string.Empty);

            if (_cache.TryGetValue(tilemap.Id, out var cache) && string.Equals(cache.Json, json, StringComparison.Ordinal))
                return cache;

            cache = Parse(json);
            _cache[tilemap.Id] = cache;
            return cache;
        }

        private static Cache Parse(string json)
        {
            var cache = new Cache
            {
                Json = json,
                Palette = new List<string>(),
                Cells = new Dictionary<(int X, int Y), int>(),
            };

            if (string.IsNullOrWhiteSpace(json))
                return cache;

            try
            {
                if (JsonNode.Parse(json) is not JsonObject root)
                    return cache;

                if (root["tiles"] is JsonArray paths)
                {
                    foreach (var node in paths)
                        cache.Palette.Add(node?.GetValue<string>() ?? string.Empty);
                }

                if (root["cells"] is JsonArray cells)
                {
                    foreach (var node in cells)
                    {
                        if (node is JsonArray { Count: 3 } cell)
                            cache.Cells[(GetInt(cell[0]), GetInt(cell[1]))] = GetInt(cell[2]);
                    }
                }
            }
            catch
            {
            }

            return cache;
        }

        private static void WriteBack(ComponentData tilemap, Cache cache)
        {
            var tiles = new JsonArray();

            foreach (var path in cache.Palette)
                tiles.Add(path);

            var cells = new JsonArray();

            foreach (var ((x, y), index) in cache.Cells)
                cells.Add(new JsonArray(x, y, index));

            string json = new JsonObject
            {
                ["tiles"] = tiles,
                ["cells"] = cells,
            }.ToJsonString();

            cache.Json = json;
            tilemap.SetString("TilesJson", json);
        }

        private static int GetInt(JsonNode node)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? (int)f : 0;
        }
    }
}
