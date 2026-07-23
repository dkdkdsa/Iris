using Iris.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace IrisEditor.Data
{
    internal static class HiddenMembers
    {
        private static readonly Dictionary<Type, IReadOnlySet<string>> _cache = new();
        private static readonly IReadOnlySet<string> _empty = new HashSet<string>();

        public static void Clear()
        {
            _cache.Clear();
        }

        public static IReadOnlySet<string> Get(Type type)
        {
            if (type == null)
                return _empty;

            if (_cache.TryGetValue(type, out var hidden))
                return hidden;

            var names = new HashSet<string>(StringComparer.Ordinal);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(prop, typeof(HideAttribute), true))
                    names.Add(prop.Name);
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(field, typeof(HideAttribute), true))
                    names.Add(field.Name);
            }

            _cache[type] = names;
            return names;
        }
    }
}
