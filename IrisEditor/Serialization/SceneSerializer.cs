using Iris.Core;
using Iris.Debugging;
using IrisEditor.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IrisEditor.Serialization
{
    internal static class SceneSerializer
    {
        private static readonly DebugChannel _log = Debug.Channel("Editor");

        public static void Save(SceneData scene, string path)
        {
            var actors = new JsonArray();

            foreach (var actor in scene.Actors)
                actors.Add(SerializeActor(actor, includeParent: true));

            var root = new JsonObject
            {
                ["actors"] = actors,
            };

            File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        public static void SavePrefab(SceneData scene, ActorData rootActor, string path)
        {
            var actors = new JsonArray();

            foreach (var actor in CollectSubtree(scene, rootActor))
            {
                var actorObj = SerializeActor(actor, includeParent: actor != rootActor);
                actorObj.Remove("prefabSource");
                actors.Add(actorObj);
            }

            var root = new JsonObject
            {
                ["actors"] = actors,
            };

            File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        public static SceneData Load(string path)
        {
            if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject root)
                throw new InvalidDataException("씬 파일 형식이 아닙니다.");

            var scene = new SceneData();

            if (root["actors"] is not JsonArray actors)
                return scene;

            scene.Actors.AddRange(ParseActors(actors));

            var ids = new HashSet<Guid>(scene.Actors.Select(x => x.Id));

            foreach (var actor in scene.Actors)
            {
                if (actor.ParentId is Guid parent && (!ids.Contains(parent) || parent == actor.Id))
                    actor.ParentId = null;
            }

            return scene;
        }

        public static List<ActorData> ParseActors(JsonArray actors)
        {
            var result = new List<ActorData>();

            foreach (var actorNode in actors)
            {
                if (actorNode is not JsonObject actorObj)
                    continue;

                var actor = new ActorData
                {
                    Id = ParseGuid(actorObj["id"]),
                    Name = actorObj["name"]?.GetValue<string>() ?? "Actor",
                    Tag = actorObj["tag"]?.GetValue<string>() ?? string.Empty,
                    ParentId = Guid.TryParse(actorObj["parent"]?.GetValue<string>(), out var parentId)
                        ? parentId
                        : null,
                    PrefabSource = actorObj["prefabSource"]?.GetValue<string>(),
                };

                if (actorObj["components"] is JsonArray components)
                {
                    foreach (var compNode in components)
                    {
                        if (compNode is not JsonObject compObj)
                            continue;

                        string typeName = compObj["type"]?.GetValue<string>();

                        if (typeName == "Iris.Core.TextureRenderer")
                            typeName = "Iris.Core.SpriteRenderer";

                        var type = ComponentCatalog.Resolve(typeName);

                        if (type == null)
                            _log.LogOnce(LogLevel.Warning, $"알 수 없는 컴포넌트 타입(데이터는 보존됨): {typeName}");

                        var properties = compObj["properties"]?.DeepClone() as JsonObject ?? new JsonObject();

                        if (type == typeof(Transform))
                            MigrateTransformKeys(properties);

                        if (type == typeof(SpriteRenderer))
                            Rename(properties, "Texture", "Sprite");

                        ComponentCatalog.ApplyMissingDefaults(properties, type);

                        actor.Components.Add(new ComponentData
                        {
                            Id = ParseGuid(compObj["id"]),
                            TargetType = type,
                            TypeName = typeName,
                            Properties = properties,
                        });
                    }
                }

                result.Add(actor);
            }

            return result;
        }

        private static JsonObject SerializeActor(ActorData actor, bool includeParent)
        {
            var components = new JsonArray();

            foreach (var comp in actor.Components)
            {
                string typeName = comp.TargetType?.FullName ?? comp.TypeName;

                if (typeName == null)
                    continue;

                components.Add(new JsonObject
                {
                    ["id"] = comp.Id.ToString(),
                    ["type"] = typeName,
                    ["properties"] = comp.Properties?.DeepClone() ?? new JsonObject(),
                });
            }

            var actorObj = new JsonObject
            {
                ["id"] = actor.Id.ToString(),
                ["name"] = actor.Name,
            };

            if (!string.IsNullOrEmpty(actor.Tag))
                actorObj["tag"] = actor.Tag;

            if (includeParent && actor.ParentId.HasValue)
                actorObj["parent"] = actor.ParentId.Value.ToString();

            if (!string.IsNullOrEmpty(actor.PrefabSource))
                actorObj["prefabSource"] = actor.PrefabSource;

            actorObj["components"] = components;
            return actorObj;
        }

        private static List<ActorData> CollectSubtree(SceneData scene, ActorData root)
        {
            var result = new List<ActorData>();
            var queue = new Queue<ActorData>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                foreach (var candidate in scene.Actors)
                {
                    if (candidate.ParentId == current.Id && !result.Contains(candidate))
                        queue.Enqueue(candidate);
                }
            }

            return result;
        }

        private static void MigrateTransformKeys(JsonObject properties)
        {
            Rename(properties, "Position", "LocalPosition");
            Rename(properties, "Rotation", "LocalRotation");
            Rename(properties, "Scale", "LocalScale");
        }

        private static void Rename(JsonObject properties, string oldKey, string newKey)
        {
            if (properties.ContainsKey(newKey) || properties[oldKey] is not JsonNode node)
                return;

            properties.Remove(oldKey);
            properties[newKey] = node;
        }

        private static Guid ParseGuid(JsonNode node)
        {
            return Guid.TryParse(node?.GetValue<string>(), out var id) ? id : Guid.NewGuid();
        }
    }
}
