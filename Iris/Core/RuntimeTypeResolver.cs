using Iris.UI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Iris.Core
{
    internal static class RuntimeTypeResolver
    {
        private static readonly List<Assembly> _assemblies = new() { typeof(Component).Assembly };
        private static Dictionary<string, Type> _components;
        private static Dictionary<string, Type> _uiObjects;

        private static readonly Dictionary<string, string> _componentAliases = new(StringComparer.Ordinal)
        {
            ["Iris.Core.TextureRenderer"] = "Iris.Core.SpriteRenderer",
        };

        static RuntimeTypeResolver()
        {
            var entry = Assembly.GetEntryAssembly();

            if (entry != null && entry != typeof(Component).Assembly)
                _assemblies.Add(entry);
        }

        public static void Register(Assembly assembly)
        {
            if (assembly == null || _assemblies.Contains(assembly))
                return;

            _assemblies.Add(assembly);
            _components = null;
            _uiObjects = null;
        }

        public static Type ResolveComponent(string fullName)
        {
            _components ??= BuildMap(typeof(Component), includeBase: false);

            if (fullName == null)
                return null;

            if (_componentAliases.TryGetValue(fullName, out var alias))
                fullName = alias;

            return _components.GetValueOrDefault(fullName);
        }

        public static Type ResolveUIObject(string fullName)
        {
            _uiObjects ??= BuildMap(typeof(UIObject), includeBase: true);
            return fullName != null ? _uiObjects.GetValueOrDefault(fullName) : null;
        }

        private static Dictionary<string, Type> BuildMap(Type baseType, bool includeBase)
        {
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);

            foreach (var assembly in _assemblies)
            {
                foreach (var type in SafeGetTypes(assembly))
                {
                    if (type.IsAbstract)
                        continue;

                    if (type.IsSubclassOf(baseType) || (includeBase && type == baseType))
                        map[type.FullName] = type;
                }
            }

            return map;
        }

        private static Type[] SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var loaded = new List<Type>();

                foreach (var type in ex.Types)
                {
                    if (type != null)
                        loaded.Add(type);
                }

                return loaded.ToArray();
            }
        }
    }
}
