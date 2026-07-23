using Silk.NET.Maths;
using System;

namespace Iris.Core
{
    public sealed class Grid : Component
    {
        public Vector2D<float> CellSize { get; set; } = Vector2D<float>.One;

        public Vector2D<float> Origin => OwnerActor?.Transform?.Position ?? Vector2D<float>.Zero;

        public Vector2D<float> GetGridPosition(Vector2D<float> position)
        {
            var local = position - Origin;

            return new Vector2D<float>(
                CellIndex(local.X, CellSize.X),
                CellIndex(local.Y, CellSize.Y));
        }

        public Vector2D<float> GetWorldPosition(Vector2D<float> cell)
        {
            var origin = Origin;

            return new Vector2D<float>(
                origin.X + (cell.X + 0.5f) * CellSize.X,
                origin.Y + (cell.Y + 0.5f) * CellSize.Y);
        }

        public Vector2D<float> Snap(Vector2D<float> position)
        {
            return GetWorldPosition(GetGridPosition(position));
        }

        public Rectangle<float> GetCellBounds(Vector2D<float> cell)
        {
            var origin = Origin;

            return new Rectangle<float>(
                origin.X + cell.X * CellSize.X,
                origin.Y + cell.Y * CellSize.Y,
                CellSize.X,
                CellSize.Y);
        }

        private static float CellIndex(float value, float size)
        {
            return size > 0f ? MathF.Floor(value / size) : 0f;
        }
    }
}
