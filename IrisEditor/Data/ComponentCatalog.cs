using Iris.Assets;
using Iris.Core;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace IrisEditor.Data
{
    internal static class ComponentCatalog
    {
        private static List<Type> _types;
        private static Assembly _gameAssembly;
        private static Dictionary<string, Type> _byFullName;
        private static readonly Dictionary<Type, JsonObject> _templates = new();
        private static readonly Dictionary<Type, Dictionary<string, Type>> _assetProperties = new();

        public static IReadOnlyList<Type> Types => _types ??= Scan();

        public static void SetGameAssembly(Assembly assembly)
        {
            _gameAssembly = assembly;
            _types = null;
            _byFullName = null;
            _templates.Clear();
            _assetProperties.Clear();
        }

        private static List<Type> Scan()
        {
            var assemblies = new List<Assembly> { typeof(Component).Assembly };

            if (_gameAssembly != null)
                assemblies.Add(_gameAssembly);

            var result = new List<Type>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in SafeGetTypes(assembly))
                {
                    if (!type.IsAbstract && type.IsSubclassOf(typeof(Component)))
                        result.Add(type);
                }
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        private static Type[] SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        public static IReadOnlyDictionary<string, Type> GetAssetProperties(Type type)
        {
            if (!_assetProperties.TryGetValue(type, out var map))
            {
                map = new Dictionary<string, Type>();

                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.GetGetMethod() == null || prop.GetSetMethod() == null)
                        continue;

                    if (typeof(IAsset).IsAssignableFrom(prop.PropertyType))
                        map[prop.Name] = prop.PropertyType;
                }

                _assetProperties[type] = map;
            }

            return map;
        }

        public static Type Resolve(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;

            if (_byFullName == null)
            {
                _byFullName = new Dictionary<string, Type>(StringComparer.Ordinal);

                foreach (var type in Types)
                    _byFullName[type.FullName] = type;
            }

            return _byFullName.GetValueOrDefault(fullName);
        }

        public static JsonObject DefaultProperties(Type type)
        {
            if (!_templates.TryGetValue(type, out var template))
            {
                template = BuildTemplate(type);
                _templates[type] = template;
            }

            return template.DeepClone().AsObject();
        }

        private static JsonObject BuildTemplate(Type type)
        {
            var result = new JsonObject();
            object instance;

            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch
            {
                return result;
            }

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;
                if (prop.GetGetMethod() == null || prop.GetSetMethod() == null)
                    continue;
                if (prop.DeclaringType == typeof(Component) || prop.DeclaringType == typeof(EngineObject))
                    continue;
                if(!IsAutoProperty(prop))
                    continue;

                try
                {
                    var node = ToJson(prop.PropertyType, prop.GetValue(instance));
                    if (node != null)
                        result[prop.Name] = node;
                }
                catch
                {
                }
            }

            return result;
        }

        private static bool IsAutoProperty(PropertyInfo property)
        {
            var getter = property.GetGetMethod(true);
            var setter = property.GetSetMethod(true);

            if (getter != null && !getter.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                return false;
            }

            if (setter != null && !setter.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                return false;
            }

            return property.DeclaringType.GetField($"<{property.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic) != null;
        }

        private static JsonNode ToJson(Type type, object value)
        {
            if (typeof(IAsset).IsAssignableFrom(type))
                return JsonValue.Create(string.Empty);

            if (type == typeof(string))
                return JsonValue.Create((string)value ?? string.Empty);

            if (type.IsEnum)
                return JsonValue.Create(value.ToString());

            return value switch
            {
                float f => JsonValue.Create(f),
                double d => JsonValue.Create((float)d),
                int i => JsonValue.Create((float)i),
                bool b => JsonValue.Create(b),
                Vector2D<float> v => new JsonArray(v.X, v.Y),
                Color c => new JsonArray(c.r, c.g, c.b, c.a),
                _ => null,
            };
        }
    }
}
