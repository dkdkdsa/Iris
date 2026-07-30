using Iris.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Iris.Core
{
    public static class SceneLoader
    {
        public static string ContentRoot { get; set; } = AppContext.BaseDirectory;

        public static void RegisterAssembly(Assembly assembly)
        {
            RuntimeTypeResolver.Register(assembly);
        }

        public static Scene Load(string path)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(ContentRoot, path);

            if (JsonNode.Parse(VirtualFileSystem.ReadAllText(fullPath)) is not JsonObject root)
                throw new InvalidDataException($"씬 파일 형식이 아닙니다: {path}");

            var scene = new Scene();

            if (root["actors"] is not JsonArray actors)
                return scene;

            var byId = new Dictionary<string, Actor>(StringComparer.OrdinalIgnoreCase);
            var parentLinks = new List<(Actor Child, string ParentId)>();
            var built = new List<Actor>();

            foreach (var actorNode in actors)
            {
                if (actorNode is not JsonObject actorObj)
                    continue;

                try
                {
                    var actor = BuildActor(scene, actorObj);
                    built.Add(actor);

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

            foreach (var actor in built)
                actor.Awake();

            return scene;
        }

        internal static Actor BuildActor(Scene scene, JsonObject actorObj)
        {
            var actor = scene.CreateActorDeferred();
            actor.Name = actorObj["name"]?.GetValue<string>() ?? "Actor";
            actor.Tag = actorObj["tag"]?.GetValue<string>() ?? string.Empty;

            if (actorObj["components"] is not JsonArray components)
                return actor;

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

                var properties = compObj["properties"] as JsonObject;

                if (type == typeof(SpriteRenderer) && properties != null &&
                    properties["Sprite"] == null && properties["Texture"] is JsonNode textureNode)
                {
                    properties.Remove("Texture");
                    properties["Sprite"] = textureNode;
                }

                try
                {
                    var component = (Component)Activator.CreateInstance(type);
                    JsonPropertyMapper.ApplyProperties(component, properties);
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
