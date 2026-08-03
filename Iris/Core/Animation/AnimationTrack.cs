using Silk.NET.Maths;
using System;

namespace Iris.Core
{
    public readonly struct Keyframe<T>
    {
        public readonly float Time;
        public readonly T Value;

        public Keyframe(float time, T value)
        {
            Time = time;
            Value = value;
        }
    }

    public abstract class AnimationTrack
    {
        public string ComponentType { get; }
        public string Property { get; }

        public abstract int KeyCount { get; }
        public abstract float Duration { get; }

        protected AnimationTrack(string componentType, string property)
        {
            ComponentType = componentType;
            Property = property;
        }

        internal abstract void Apply(Component target, float time);
    }

    public abstract class AnimationTrack<T> : AnimationTrack
    {
        private readonly Keyframe<T>[] _keys;

        private Action<object, T> _setter;
        private Type _bound;
        private bool _failed;

        protected AnimationTrack(string componentType, string property, Keyframe<T>[] keys)
            : base(componentType, property)
        {
            _keys = keys ?? Array.Empty<Keyframe<T>>();
        }

        public override int KeyCount => _keys.Length;

        public override float Duration => _keys.Length > 0 ? _keys[_keys.Length - 1].Time : 0f;

        public Keyframe<T> GetKey(int index) => _keys[index];

        protected abstract T Interpolate(in T from, in T to, float amount);

        public T Evaluate(float time)
        {
            if (_keys.Length == 0)
                return default;

            int index = FindIndex(time);

            if (index < 0)
                return _keys[0].Value;

            if (index >= _keys.Length - 1)
                return _keys[_keys.Length - 1].Value;

            var from = _keys[index];
            var to = _keys[index + 1];

            float span = to.Time - from.Time;

            if (span <= 0f)
                return to.Value;

            return Interpolate(from.Value, to.Value, (time - from.Time) / span);
        }

        internal override void Apply(Component target, float time)
        {
            if (_keys.Length == 0 || target == null)
                return;

            var type = target.GetType();

            if (_bound != type)
            {
                _bound = type;
                _setter = PropertyBinder.Create<T>(type, Property);
                _failed = _setter == null;
            }

            if (_failed)
                return;

            _setter(target, Evaluate(time));
        }

        private int FindIndex(float time)
        {
            int low = 0;
            int high = _keys.Length - 1;

            while (low <= high)
            {
                int mid = (low + high) >> 1;

                if (_keys[mid].Time <= time)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            return high;
        }
    }

    public sealed class SpriteTrack : AnimationTrack<Sprite>
    {
        public SpriteTrack(string componentType, string property, Keyframe<Sprite>[] keys)
            : base(componentType, property, keys)
        {
        }

        protected override Sprite Interpolate(in Sprite from, in Sprite to, float amount) => from;
    }

    public sealed class FloatTrack : AnimationTrack<float>
    {
        public FloatTrack(string componentType, string property, Keyframe<float>[] keys)
            : base(componentType, property, keys)
        {
        }

        protected override float Interpolate(in float from, in float to, float amount)
        {
            return from + (to - from) * amount;
        }
    }

    public sealed class Vector2Track : AnimationTrack<Vector2D<float>>
    {
        public Vector2Track(string componentType, string property, Keyframe<Vector2D<float>>[] keys)
            : base(componentType, property, keys)
        {
        }

        protected override Vector2D<float> Interpolate(in Vector2D<float> from, in Vector2D<float> to, float amount)
        {
            return new Vector2D<float>(
                from.X + (to.X - from.X) * amount,
                from.Y + (to.Y - from.Y) * amount);
        }
    }

    public sealed class ColorTrack : AnimationTrack<Color>
    {
        public ColorTrack(string componentType, string property, Keyframe<Color>[] keys)
            : base(componentType, property, keys)
        {
        }

        protected override Color Interpolate(in Color from, in Color to, float amount)
        {
            return new Color(
                Blend(from.r, to.r, amount),
                Blend(from.g, to.g, amount),
                Blend(from.b, to.b, amount),
                Blend(from.a, to.a, amount));
        }

        private static byte Blend(byte from, byte to, float amount)
        {
            float value = from + (to - from) * amount;

            return (byte)(value < 0f ? 0f : value > 255f ? 255f : value + 0.5f);
        }
    }
}
