using Iris.Rendering;
using Silk.NET.Maths;

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

            foreach (var (cell, tile) in map.Tiles)
            {
                if (tile?.Texture == null)
                    continue;

                system.Submit(new RenderCommand
                {
                    texture = tile.Texture,
                    src = tile.SrcRect,
                    dest = new Rectangle<float>(
                        origin.X + cell.X * cellSize.X,
                        origin.Y + cell.Y * cellSize.Y,
                        cellSize.X, cellSize.Y),
                    order = Order,
                    color = Color,
                });
            }
        }
    }
}
