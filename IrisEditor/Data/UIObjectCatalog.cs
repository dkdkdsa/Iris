using Iris.Assets;
using Iris.Attributes;
using Iris.Core;
using Iris.UI;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace IrisEditor.Data
{
    internal static class UIObjectCatalog
    {
        private static List<Type> _types;
        private static IReadOnlyList<Assembly> _gameAssemblies = Array.Empty<Assembly>();
        private static Dictionary<string, Type> _byFullName;
        private static readonly Dictionary<Type, JsonObject> _templates = new();
        private static readonly Dictionary<Type, Dictionary<string, Type>> _assetProperties = new();

        public static IReadOnlyList<Type> Types => _types ??= Scan();

        public static void SetGameAssemblies(IReadOnlyList<Assembly> assemblies)
        {
            _gameAssemblies = assemblies ?? Array.Empty<Assembly>();
            _types = null;
            _byFullName = null;
            _templates.Clear();
            _assetProperties.Clear();
            HiddenMembers.Clear();
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

                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.IsInitOnly)
                        continue;

                    if (typeof(IAsset).IsAssignableFrom(field.FieldType))
                        map[field.Name] = field.FieldType;
                }

                _assetProperties[type] = map;
            }

            return map;
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

        public static void ApplyMissingDefaults(JsonObject properties, Type type)
        {
            if (properties == null || type == null)
                return;

            PropertyMerge.Apply(properties, DefaultProperties(type));
        }

        private static List<Type> Scan()
        {
            var assemblies = new List<Assembly> { typeof(UIObject).Assembly };

            foreach (var assembly in _gameAssemblies)
            {
                if (assembly != null && !assemblies.Contains(assembly))
                    assemblies.Add(assembly);
            }

            var result = new List<Type>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in SafeGetTypes(assembly))
                {
                    if (type.IsAbstract)
                        continue;

                    if (type == typeof(UIObject) || type.IsSubclassOf(typeof(UIObject)))
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

            var entries = new List<(long Order, string Name, JsonNode Node)>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;
                if (prop.GetGetMethod() == null || prop.GetSetMethod() == null)
                    continue;
                if (!IsAutoProperty(prop) && !Attribute.IsDefined(prop, typeof(ShowAttribute), true))
                    continue;

                try
                {
                    var node = ToJson(prop.PropertyType, prop.GetValue(instance));
                    if (node != null && seen.Add(prop.Name))
                        entries.Add((MemberOrder.KeyOf(prop), prop.Name, node));
                }
                catch
                {
                }
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.IsInitOnly || field.IsLiteral)
                    continue;

                try
                {
                    var node = ToJson(field.FieldType, field.GetValue(instance));
                    if (node != null && seen.Add(field.Name))
                        entries.Add((MemberOrder.KeyOf(field), field.Name, node));
                }
                catch
                {
                }
            }

            entries.Sort((a, b) => a.Order.CompareTo(b.Order));

            foreach (var entry in entries)
                result[entry.Name] = entry.Node;

            return result;
        }

        private static bool IsAutoProperty(PropertyInfo property)
        {
            var getter = property.GetGetMethod(true);
            var setter = property.GetSetMethod(true);

            if (getter != null && !getter.IsDefined(typeof(CompilerGeneratedAttribute), false))
                return false;

            if (setter != null && !setter.IsDefined(typeof(CompilerGeneratedAttribute), false))
                return false;

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
                Vector2D<int> vi => new JsonArray((float)vi.X, (float)vi.Y),
                Color c => new JsonArray((float)c.r, (float)c.g, (float)c.b, (float)c.a),
                _ => null,
            };
        }
    }
}
