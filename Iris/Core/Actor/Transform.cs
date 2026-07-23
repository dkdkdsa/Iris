using Silk.NET.Maths;
using System;

namespace Iris.Core
{
    public sealed class Transform : Component
    {
        public Vector2D<float> LocalPosition { get; set; }
        public float LocalRotation { get; set; }
        public Vector2D<float> LocalScale { get; set; } = Vector2D<float>.One;

        private Transform ParentTransform => OwnerActor?.Parent?.Transform;

        public Vector2D<float> Position
        {
            get
            {
                var parent = ParentTransform;

                if (parent == null)
                    return LocalPosition;

                var parentScale = parent.Scale;
                var scaled = new Vector2D<float>(LocalPosition.X * parentScale.X, LocalPosition.Y * parentScale.Y);

                return parent.Position + Rotate(scaled, parent.Rotation);
            }
            set
            {
                var parent = ParentTransform;

                if (parent == null)
                {
                    LocalPosition = value;
                    return;
                }

                var parentScale = parent.Scale;
                var delta = Rotate(value - parent.Position, -parent.Rotation);

                LocalPosition = new Vector2D<float>(
                    parentScale.X != 0f ? delta.X / parentScale.X : 0f,
                    parentScale.Y != 0f ? delta.Y / parentScale.Y : 0f);
            }
        }

        public float Rotation
        {
            get
            {
                var parent = ParentTransform;
                return parent == null ? LocalRotation : parent.Rotation + LocalRotation;
            }
            set
            {
                var parent = ParentTransform;
                LocalRotation = parent == null ? value : value - parent.Rotation;
            }
        }

        public Vector2D<float> Scale
        {
            get
            {
                var parent = ParentTransform;

                if (parent == null)
                    return LocalScale;

                var parentScale = parent.Scale;

                return new Vector2D<float>(LocalScale.X * parentScale.X, LocalScale.Y * parentScale.Y);
            }
            set
            {
                var parent = ParentTransform;

                if (parent == null)
                {
                    LocalScale = value;
                    return;
                }

                var parentScale = parent.Scale;

                LocalScale = new Vector2D<float>(
                    parentScale.X != 0f ? value.X / parentScale.X : 0f,
                    parentScale.Y != 0f ? value.Y / parentScale.Y : 0f);
            }
        }

        internal static Vector2D<float> Rotate(Vector2D<float> vector, float degrees)
        {
            if (degrees == 0f)
                return vector;

            float rad = degrees * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);

            return new Vector2D<float>(
                vector.X * cos + vector.Y * sin,
                -vector.X * sin + vector.Y * cos);
        }
    }
}
