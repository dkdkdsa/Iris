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

        /// <summary>커스텀 컴포넌트/UI 오브젝트가 들어있는 어셈블리를 타입 해석 대상에 추가한다.</summary>
        public static void RegisterAssembly(Assembly assembly)
        {
            RuntimeTypeResolver.Register(assembly);
        }

        public static Scene Load(string path)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(ContentRoot, path);

            if (JsonNode.Parse(File.ReadAllText(fullPath)) is not JsonObject root)
                throw new InvalidDataException($"씬 파일 형식이 아닙니다: {path}");

            var scene = new Scene();

            if (root["actors"] is not JsonArray actors)
                return scene;

            var byId = new Dictionary<string, Actor>(StringComparer.OrdinalIgnoreCase);
            var parentLinks = new List<(Actor Child, string ParentId)>();

            foreach (var actorNode in actors)
            {
                if (actorNode is not JsonObject actorObj)
                    continue;

                try
                {
                    var actor = BuildActor(scene, actorObj);

                    string id = actorObj["id"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(id))
                        byId[id] = actor;

                    string parentId = actorObj["parent"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(parentId))
                        parentLinks.Add((actor, parentId));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Iris] 액터 로드 실패: {ex.Message}");
                }
            }

            foreach (var (child, parentId) in parentLinks)
            {
                if (byId.TryGetValue(parentId, out var parent))
                    child.SetParent(parent, worldPositionStays: false);
                else
                    Console.WriteLine($"[Iris] 부모 액터를 찾지 못해 루트로 둠: {child.Name}");
            }

            return scene;
        }

        internal static Actor BuildActor(Scene scene, JsonObject actorObj)
        {
            var actor = scene.CreateActor();
            actor.Name = actorObj["name"]?.GetValue<string>() ?? "Actor";

            if (actorObj["components"] is not JsonArray components)
                return actor;

            // Transform을 가장 먼저 적용한다 - 뒤 컴포넌트의 OnAttached가 Transform을 읽는다(예: Rigidbody).
            foreach (var compNode in components)
            {
                if (compNode is JsonObject transformObj &&
                    RuntimeTypeResolver.ResolveComponent(transformObj["type"]?.GetValue<string>()) == typeof(Transform))
                {
                    JsonPropertyMapper.ApplyProperties(actor.Transform, transformObj["properties"] as JsonObject);
                    break;
                }
            }

            foreach (var compNode in components)
            {
                if (compNode is not JsonObject compObj)
                    continue;

                string typeName = compObj["type"]?.GetValue<string>();
                var type = RuntimeTypeResolver.ResolveComponent(typeName);

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
                    JsonPropertyMapper.ApplyProperties(component, compObj["properties"] as JsonObject);
                    actor.AddComponent(component);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Iris] 컴포넌트 로드 실패({type.Name}): {ex.Message}");
                }
            }

            return actor;
        }
    }
}
