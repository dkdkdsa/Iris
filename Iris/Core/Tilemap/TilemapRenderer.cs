using Iris.Rendering;
using Silk.NET.Maths;
using System;

namespace Iris.Core
{
    public sealed class TilemapRenderer : RendererBase
    {
        public int Order { get; set; }
        public Color Color { get; set; } = new Color(255, 255, 255, 255);

        protected override void Render()
        {
            var map = GetComponent<Tilemap>();

            if (map == null || map.Tiles.Count == 0)
                return;

            var grid = map.FindGrid();
            var origin = grid?.Origin ?? Transform.Position;
            var cellSize = grid?.CellSize ?? Vector2D<float>.One;

            if (cellSize.X <= 0f || cellSize.Y <= 0f)
                return;

            Vector2D<int> min = default;
            Vector2D<int> max = default;

            bool bounded = system.CullingEnabled && TryGetVisibleCells(origin, cellSize, out min, out max);

            if (bounded)
            {
                long span = (long)(max.X - min.X + 1) * (max.Y - min.Y + 1);

                if (span < map.Tiles.Count)
                {
                    for (int x = min.X; x <= max.X; x++)
                    {
                        for (int y = min.Y; y <= max.Y; y++)
                        {
                            if (map.Tiles.TryGetValue(new Vector2D<int>(x, y), out var found))
                                SubmitTile(found, origin, cellSize, x, y);
                        }
                    }

                    return;
                }
            }

            foreach (var (cell, tile) in map.Tiles)
            {
                if (bounded && (cell.X < min.X || cell.X > max.X || cell.Y < min.Y || cell.Y > max.Y))
                    continue;

                SubmitTile(tile, origin, cellSize, cell.X, cell.Y);
            }
        }

        private void SubmitTile(Tile tile, Vector2D<float> origin, Vector2D<float> cellSize, int x, int y)
        {
            if (tile?.Texture == null)
                return;

            system.Submit(new RenderCommand
            {
                texture = tile.Texture,
                src = tile.SrcRect,
                dest = new Rectangle<float>(
                    origin.X + x * cellSize.X,
                    origin.Y + y * cellSize.Y,
                    cellSize.X, cellSize.Y),
                order = Order,
                color = Color,
            });
        }

        private static bool TryGetVisibleCells(
            Vector2D<float> origin, Vector2D<float> cellSize, out Vector2D<int> min, out Vector2D<int> max)
        {
            min = default;
            max = default;

            var camera = Camera.Main;

            if (camera == null)
                return false;

            var bounds = camera.WorldBounds;

            if (bounds.Size.X <= 0f || bounds.Size.Y <= 0f)
                return false;

            int left = FloorToCell((bounds.Origin.X - origin.X) / cellSize.X);
            int right = FloorToCell((bounds.Origin.X + bounds.Size.X - origin.X) / cellSize.X);
            int bottom = FloorToCell((bounds.Origin.Y - origin.Y) / cellSize.Y);
            int top = FloorToCell((bounds.Origin.Y + bounds.Size.Y - origin.Y) / cellSize.Y);

            min = new Vector2D<int>(left - 1, bottom - 1);
            max = new Vector2D<int>(right + 1, top + 1);

            return true;
        }

        private static int FloorToCell(float value)
        {
            if (float.IsNaN(value))
                return 0;

            float floored = MathF.Floor(value);

            if (floored <= int.MinValue + 2)
                return int.MinValue + 2;

            if (floored >= int.MaxValue - 2)
                return int.MaxValue - 2;

            return (int)floored;
        }
    }
}
