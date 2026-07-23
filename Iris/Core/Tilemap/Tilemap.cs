using Iris.Assets;
using Iris.Attributes;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace Iris.Core
{
    public class Tilemap : Component
    {
        private readonly Dictionary<Vector2D<int>, Tile> _tiles = new();
        private string _serialized = string.Empty;
        private bool _parsed = true;

        [Hide]
        public string TilesJson
        {
            get => _serialized;
            set
            {
                _serialized = value ?? string.Empty;
                _parsed = false;
            }
        }

        public IReadOnlyDictionary<Vector2D<int>, Tile> Tiles
        {
            get
            {
                EnsureParsed();
                return _tiles;
            }
        }

        public Tile GetTile(Vector2D<int> cell)
        {
            EnsureParsed();
            return _tiles.GetValueOrDefault(cell);
        }

        public void SetTile(Vector2D<int> cell, Tile tile)
        {
            EnsureParsed();

            if (tile == null)
                _tiles.Remove(cell);
            else
                _tiles[cell] = tile;
        }

        public bool RemoveTile(Vector2D<int> cell)
        {
            EnsureParsed();
            return _tiles.Remove(cell);
        }

        public void Clear()
        {
            EnsureParsed();
            _tiles.Clear();
        }

        public List<TileData> GetTiles()
        {
            EnsureParsed();

            var result = new List<TileData>(_tiles.Count);

            foreach (var (cell, tile) in _tiles)
                result.Add(new TileData { Cell = cell, Tile = tile });

            return result;
        }

        public Grid FindGrid()
        {
            for (var actor = OwnerActor; actor != null; actor = actor.Parent)
            {
                var grid = actor.GetComponent<Grid>();

                if (grid != null)
                    return grid;
            }

            return null;
        }

        private void EnsureParsed()
        {
            if (_parsed)
                return;

            _parsed = true;
            _tiles.Clear();

            if (string.IsNullOrWhiteSpace(_serialized))
                return;

            try
            {
                if (JsonNode.Parse(_serialized) is not JsonObject root)
                    return;

                var palette = new List<Tile>();

                if (root["tiles"] is JsonArray paths)
                {
                    foreach (var node in paths)
                        palette.Add(LoadTile(node?.GetValue<string>()));
                }

                if (root["cells"] is not JsonArray cells)
                    return;

                foreach (var node in cells)
                {
                    if (node is not JsonArray { Count: 3 } cell)
                        continue;

                    int index = GetInt(cell[2]);

                    if (index < 0 || index >= palette.Count || palette[index] == null)
                        continue;

                    _tiles[new Vector2D<int>(GetInt(cell[0]), GetInt(cell[1]))] = palette[index];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Iris] 타일맵 데이터 파싱 실패: {ex.Message}");
            }
        }

        private static Tile LoadTile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(SceneLoader.ContentRoot, path);
                return AssetManager.Load<Tile>(fullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Iris] 타일 로드 실패({path}): {ex.Message}");
                return null;
            }
        }

        private static int GetInt(JsonNode node)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? (int)f : 0;
        }
    }
}
