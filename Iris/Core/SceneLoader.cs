using Iris.Assets;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Iris.Core
{
    /// <summary>
    /// 에디터가 저장한 .scene 파일을 읽어 살아있는 Scene을 만든다.
    /// 호출마다 새 Scene을 반환한다(캐시 없음 - 레벨 재시작 대응).
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>에셋 상대 경로의 기준 폴더. 기본값은 실행 파일 위치.</summary>
        public static string ContentRoot { get; set; } = AppContext.BaseDirectory;

        private static readonly List<Assembly> _assemblies = new() { typeof(Component).Assembly };
        private static Dictionary<string, Type> _componentTypes;

        static SceneLoader()
        {
            var entry = Assembly.GetEntryAssembly();
            if (entry != null && entry != typeof(Component).Assembly)
                _assemblies.Add(entry);
        }

        /// <summary>커스텀 컴포넌트가 들어있는 어셈블리를 타입 해석 대상에 추가한다.</summary>
        public static void RegisterAssembly(Assembly assembly)
        {
            if (assembly == null || _assemblies.Contains(assembly))
                return;

            _assemblies.Add(assembly);
            _componentTypes = null;
        }

        public static Scene Load(string path)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(ContentRoot, path);

            if (JsonNode.Parse(File.ReadAllText(fullPath)) is not JsonObject root)
                throw new InvalidDataException($"씬 파일 형식이 아닙니다: {path}");

            var scene = new Scene();

            if (root["actors"] is not JsonArray actors)
                return scene;

            foreach (var actorNode in actors)
            {
                if (actorNode is not JsonObject actorObj)
                    continue;

                try
                {
                    LoadActor(scene, actorObj);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Iris] 액터 로드 실패: {ex.Message}");
                }
            }

            return scene;
        }

        private static void LoadActor(Scene scene, JsonObject actorObj)
        {
            var actor = scene.CreateActor();
            actor.Name = actorObj["name"]?.GetValue<string>() ?? "Actor";

            if (actorObj["components"] is not JsonArray components)
                return;

            // Transform을 가장 먼저 적용한다 - 뒤 컴포넌트의 OnAttached가 Transform을 읽는다(예: Rigidbody).
            foreach (var compNode in components)
            {
                if (compNode is JsonObject transformObj &&
                    ResolveType(transformObj["type"]?.GetValue<string>()) == typeof(Transform))
                {
                    ApplyProperties(actor.Transform, transformObj["properties"] as JsonObject);
                    break;
                }
            }

            foreach (var compNode in components)
            {
                if (compNode is not JsonObject compObj)
                    continue;

                string typeName = compObj["type"]?.GetValue<string>();
                var type = ResolveType(typeName);

                if (type == null)
                {
                    Console.WriteLine($"[Iris] 알 수 없는 컴포넌트 타입을 건너뜀: {typeName}");
                    continue;
                }

                if (type == typeof(Transform))
                    continue;

                try
                {
                    var component = (Component)Activator.CreateInstance(type);
                    ApplyProperties(component, compObj["properties"] as JsonObject);
                    actor.AddComponent(component);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Iris] 컴포넌트 로드 실패({type.Name}): {ex.Message}");
                }
            }
        }

        private static void ApplyProperties(Component component, JsonObject properties)
        {
            if (properties == null)
                return;

            var type = component.GetType();

            foreach (var pair in properties)
            {
                try
                {
                    var prop = type.GetProperty(pair.Key, BindingFlags.Public | BindingFlags.Instance);
                    if (prop == null || prop.GetSetMethod() == null)
                        continue;

                    if (TryFromJson(prop.PropertyType, pair.Value, out var value))
                        prop.SetValue(component, value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Iris] 프로퍼티 적용 실패({type.Name}.{pair.Key}): {ex.Message}");
                }
            }
        }

        private static bool TryFromJson(Type targetType, JsonNode node, out object value)
        {
            value = null;

            if (node == null)
                return false;

            if (typeof(IAsset).IsAssignableFrom(targetType))
            {
                string assetPath = node is JsonValue av && av.TryGetValue(out string ap) ? ap : null;

                if (string.IsNullOrWhiteSpace(assetPath))
                    return true;

                value = LoadAsset(targetType, assetPath);
                return true;
            }

            if (targetType == typeof(string))
            {
                if (node is JsonValue sv && sv.TryGetValue(out string s))
                {
                    value = s;
                    return true;
                }

                return false;
            }

            if (targetType.IsEnum)
            {
                if (node is JsonValue ev && ev.TryGetValue(out string name) &&
                    Enum.TryParse(targetType, name, true, out var parsed))
                {
                    value = parsed;
                    return true;
                }

                return false;
            }

            if (targetType == typeof(bool))
            {
                if (node is JsonValue bv && bv.TryGetValue(out bool b))
                {
                    value = b;
                    return true;
                }

                return false;
            }

            if (targetType == typeof(Vector2D<float>))
            {
                if (node is JsonArray { Count: 2 } arr)
                {
                    value = new Vector2D<float>(GetFloat(arr[0]), GetFloat(arr[1]));
                    return true;
                }

                return false;
            }

            if (targetType == typeof(Color))
            {
                if (node is JsonArray { Count: 4 } arr)
                {
                    value = new Color((byte)GetFloat(arr[0]), (byte)GetFloat(arr[1]), (byte)GetFloat(arr[2]), (byte)GetFloat(arr[3]));
                    return true;
                }

                return false;
            }

            if (targetType.IsPrimitive)
            {
                if (node is JsonValue nv && nv.TryGetValue(out float f))
                {
                    value = Convert.ChangeType(f, targetType);
                    return true;
                }

                return false;
            }

            return false;
        }

        private static float GetFloat(JsonNode node)
        {
            return node is JsonValue v && v.TryGetValue(out float f) ? f : 0f;
        }

        private static object LoadAsset(Type assetType, string relativePath)
        {
            string fullPath = Path.IsPathRooted(relativePath) ? relativePath : Path.Combine(ContentRoot, relativePath);

            return typeof(AssetManager).GetMethod(nameof(AssetManager.Load))
                .MakeGenericMethod(assetType)
                .Invoke(null, new object[] { fullPath });
        }

        private static Type ResolveType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;

            if (_componentTypes == null)
            {
                _componentTypes = new Dictionary<string, Type>(StringComparer.Ordinal);

                foreach (var assembly in _assemblies)
                {
                    foreach (var type in SafeGetTypes(assembly))
                    {
                        if (!type.IsAbstract && type.IsSubclassOf(typeof(Component)))
                            _componentTypes[type.FullName] = type;
                    }
                }
            }

            return _componentTypes.GetValueOrDefault(fullName);
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
