using Iris.Assets;
using Iris.Debugging;
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
        private static string _contentRoot = Normalize(AppContext.BaseDirectory);

        public static string ContentRoot
        {
            get
            {
                return _contentRoot;
            }
            set
            {
                string normalized = Normalize(value);

                if (string.Equals(_contentRoot, normalized, StringComparison.OrdinalIgnoreCase))
                    return;

                _contentRoot = normalized;
                AssetManager.UnloadAll();
            }
        }

        private static string Normalize(string path)
        {
            string target = string.IsNullOrWhiteSpace(path) ? AppContext.BaseDirectory : path;

            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(target));
        }

        public static void RegisterAssembly(Assembly assembly)
        {
            RuntimeTypeResolver.Register(assembly);
        }

        public static Scene Load(string path)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(ContentRoot, path);

            if (JsonNode.Parse(VirtualFileSystem.ReadAllText(fullPath)) is not JsonObject root)
                throw new InvalidDataException($"Not a scene file: {path}");

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
                    Debug.LogException("Failed to load actor", ex);
                }
            }

            foreach (var (child, parentId) in parentLinks)
            {
                if (byId.TryGetValue(parentId, out var parent))
                    child.SetParent(parent, worldPositionStays: false);
                else
                    Debug.LogWarning($"Parent actor not found; keeping at root: {child.Name}", child);
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
            actor.Active = ReadBool(actorObj["active"], true);

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
                    Debug.LogOnce(LogLevel.Warning, $"Skipping unknown component type: {typeName}");
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
                    component.Enabled = ReadBool(compObj["enabled"], true);
                    actor.AddComponent(component);
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce($"Failed to load component ({type.Name})", ex);
                }
            }

            return actor;
        }

        private static bool ReadBool(JsonNode node, bool fallback)
        {
            return node is JsonValue value && value.TryGetValue(out bool result) ? result : fallback;
        }
    }
}
