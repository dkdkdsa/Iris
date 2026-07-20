using IrisEditor.Data;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IrisEditor.Serialization
{
    internal static class SceneSerializer
    {
        public static void Save(SceneData scene, string path)
        {
            var actors = new JsonArray();

            foreach (var actor in scene.Actors)
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

                actors.Add(new JsonObject
                {
                    ["id"] = actor.Id.ToString(),
                    ["name"] = actor.Name,
                    ["components"] = components,
                });
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

            foreach (var actorNode in actors)
            {
                if (actorNode is not JsonObject actorObj)
                    continue;

                var actor = new ActorData
                {
                    Id = ParseGuid(actorObj["id"]),
                    Name = actorObj["name"]?.GetValue<string>() ?? "Actor",
                };

                if (actorObj["components"] is JsonArray components)
                {
                    foreach (var compNode in components)
                    {
                        if (compNode is not JsonObject compObj)
                            continue;

                        string typeName = compObj["type"]?.GetValue<string>();
                        var type = ComponentCatalog.Resolve(typeName);

                        if (type == null)
                            Console.WriteLine($"[에디터] 알 수 없는 컴포넌트 타입(데이터는 보존됨): {typeName}");

                        actor.Components.Add(new ComponentData
                        {
                            Id = ParseGuid(compObj["id"]),
                            TargetType = type,
                            TypeName = typeName,
                            Properties = compObj["properties"]?.DeepClone() ?? new JsonObject(),
                        });
                    }
                }

                scene.Actors.Add(actor);
            }

            return scene;
        }

        private static Guid ParseGuid(JsonNode node)
        {
            return Guid.TryParse(node?.GetValue<string>(), out var id) ? id : Guid.NewGuid();
        }
    }
}
