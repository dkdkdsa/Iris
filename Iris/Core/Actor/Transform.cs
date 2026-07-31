using Iris.Attributes;
using Silk.NET.Maths;
using System;

namespace Iris.Core
{
    public sealed class Transform : Component
    {
        private Vector2D<float> _localPosition;
        private float _localRotation;
        private Vector2D<float> _localScale = Vector2D<float>.One;

        private Vector2D<float> _worldPosition;
        private float _worldRotation;
        private Vector2D<float> _worldScale = Vector2D<float>.One;
        private bool _dirty = true;

        private Transform ParentTransform => OwnerActor?.Parent?.Transform;

        [Show]
        public Vector2D<float> LocalPosition
        {
            get
            {
                return _localPosition;
            }
            set
            {
                _localPosition = value;
                MarkDirty();
            }
        }

        [Show]
        public float LocalRotation
        {
            get
            {
                return _localRotation;
            }
            set
            {
                _localRotation = value;
                MarkDirty();
            }
        }

        [Show]
        public Vector2D<float> LocalScale
        {
            get
            {
                return _localScale;
            }
            set
            {
                _localScale = value;
                MarkDirty();
            }
        }

        public Vector2D<float> Position
        {
            get
            {
                EnsureWorld();
                return _worldPosition;
            }
            set
            {
                var parent = ParentTransform;

                if (parent == null)
                {
                    LocalPosition = value;
                    return;
                }

                parent.EnsureWorld();

                var delta = Rotate(value - parent._worldPosition, -parent._worldRotation);

                LocalPosition = new Vector2D<float>(
                    parent._worldScale.X != 0f ? delta.X / parent._worldScale.X : 0f,
                    parent._worldScale.Y != 0f ? delta.Y / parent._worldScale.Y : 0f);
            }
        }

        public float Rotation
        {
            get
            {
                EnsureWorld();
                return _worldRotation;
            }
            set
            {
                var parent = ParentTransform;

                if (parent == null)
                {
                    LocalRotation = value;
                    return;
                }

                parent.EnsureWorld();

                LocalRotation = value - parent._worldRotation;
            }
        }

        public Vector2D<float> Scale
        {
            get
            {
                EnsureWorld();
                return _worldScale;
            }
            set
            {
                var parent = ParentTransform;

                if (parent == null)
                {
                    LocalScale = value;
                    return;
                }

                parent.EnsureWorld();

                LocalScale = new Vector2D<float>(
                    parent._worldScale.X != 0f ? value.X / parent._worldScale.X : 0f,
                    parent._worldScale.Y != 0f ? value.Y / parent._worldScale.Y : 0f);
            }
        }

        internal void MarkDirty()
        {
            if (_dirty)
                return;

            _dirty = true;

            var actor = OwnerActor;

            if (actor == null)
                return;

            var children = actor.Children;

            for (int i = 0; i < children.Count; i++)
                children[i].Transform?.MarkDirty();
        }

        private void EnsureWorld()
        {
            if (!_dirty)
                return;

            var parent = ParentTransform;

            if (parent == null)
            {
                _worldPosition = _localPosition;
                _worldRotation = _localRotation;
                _worldScale = _localScale;
            }
            else
            {
                parent.EnsureWorld();

                _worldScale = new Vector2D<float>(
                    _localScale.X * parent._worldScale.X,
                    _localScale.Y * parent._worldScale.Y);

                _worldRotation = parent._worldRotation + _localRotation;

                var scaled = new Vector2D<float>(
                    _localPosition.X * parent._worldScale.X,
                    _localPosition.Y * parent._worldScale.Y);

                _worldPosition = parent._worldPosition + Rotate(scaled, parent._worldRotation);
            }

            _dirty = false;
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
