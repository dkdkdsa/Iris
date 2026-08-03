using Iris.Debugging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Iris.Core
{
    internal static class PropertyBinder
    {
        private static readonly Dictionary<(Type Type, string Property), Delegate> _cache = new();

        private static readonly MethodInfo _factory = typeof(PropertyBinder)
            .GetMethod(nameof(CreateTyped), BindingFlags.NonPublic | BindingFlags.Static);

        public static Action<object, T> Create<T>(Type type, string property)
        {
            var key = (type, property);

            if (_cache.TryGetValue(key, out var cached))
                return (Action<object, T>)cached;

            var setter = Build<T>(type, property);
            _cache[key] = setter;

            return setter;
        }

        private static Action<object, T> Build<T>(Type type, string property)
        {
            if (type == null || string.IsNullOrEmpty(property))
                return null;

            var info = type.GetProperty(property, BindingFlags.Public | BindingFlags.Instance);

            if (info == null || info.GetSetMethod() == null)
            {
                Debug.LogOnce(LogLevel.Warning, $"Animation target has no settable property: {type.Name}.{property}");
                return null;
            }

            if (info.PropertyType.IsAssignableFrom(typeof(T)))
                return MakeSetter<T>(type, info);

            if (typeof(T) == typeof(float) && info.PropertyType == typeof(int))
            {
                var target = MakeSetter<int>(type, info);

                if (target == null)
                    return null;

                Action<object, float> rounded = (instance, value) => target(instance, (int)MathF.Round(value));

                return (Action<object, T>)(object)rounded;
            }

            Debug.LogOnce(LogLevel.Warning,
                $"Animation track type mismatch: {type.Name}.{property} is {info.PropertyType.Name}, track supplies {typeof(T).Name}");

            return null;
        }

        private static Action<object, TValue> MakeSetter<TValue>(Type type, PropertyInfo info)
        {
            try
            {
                return (Action<object, TValue>)_factory
                    .MakeGenericMethod(type, typeof(TValue))
                    .Invoke(null, new object[] { info.GetSetMethod() });
            }
            catch (Exception ex)
            {
                Debug.LogExceptionOnce($"Failed to bind {type.Name}.{info.Name}", ex);
                return null;
            }
        }

        private static Action<object, TValue> CreateTyped<TTarget, TValue>(MethodInfo setter) where TTarget : class
        {
            var typed = (Action<TTarget, TValue>)Delegate.CreateDelegate(typeof(Action<TTarget, TValue>), setter);

            return (target, value) => typed((TTarget)target, value);
        }
    }
}
