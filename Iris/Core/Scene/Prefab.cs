using Iris.Assets;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Iris.Core
{
    public sealed class Prefab : IAsset
    {
        private readonly JsonArray _actors;

        internal Prefab(JsonArray actors)
        {
            _actors = actors ?? new JsonArray();
        }

        public Actor Instantiate()
        {
            return Instantiate(null, null);
        }

        public Actor Instantiate(Vector2D<float> position)
        {
            return Instantiate(null, position);
        }

        public Actor Instantiate(Actor parent, Vector2D<float>? position = null)
        {
            var scene = SystemManager.Instance?.GetSystem<SceneSystem>()?.ActiveScene;

            if (scene == null)
            {
                Console.WriteLine("[Iris] 활성 씬이 없어 프리팹을 생성할 수 없습니다.");
                return null;
            }

            var byId = new Dictionary<string, Actor>(StringComparer.OrdinalIgnoreCase);
            var links = new List<(Actor Child, string ParentId)>();
            var roots = new List<Actor>();

            foreach (var node in _actors)
            {
                if (node is not JsonObject actorObj)
                    continue;

                try
                {
                    var actor = SceneLoader.BuildActor(scene, actorObj);

                    string id = actorObj["id"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(id))
                        byId[id] = actor;

                    string parentId = actorObj["parent"]?.GetValue<string>();

                    if (!string.IsNullOrEmpty(parentId))
                        links.Add((actor, parentId));
                    else
                        roots.Add(actor);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Iris] 프리팹 액터 생성 실패: {ex.Message}");
                }
            }

            foreach (var (child, parentId) in links)
            {
                if (byId.TryGetValue(parentId, out var linkParent))
                    child.SetParent(linkParent, worldPositionStays: false);
                else
                    roots.Add(child);
            }

            var root = roots.Count > 0 ? roots[0] : null;

            if (root != null)
            {
                if (parent != null)
                    root.SetParent(parent, worldPositionStays: false);

                if (position.HasValue)
                    root.Transform.Position = position.Value;
            }

            return root;
        }

        public void Dispose()
        {
        }
    }
}
