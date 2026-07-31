using Iris.Core;
using IrisEditor.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IrisEditor.Serialization
{
    internal static class AnimatorControllerSerializer
    {
        private static readonly JsonSerializerOptions _writeOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static AnimatorGraph Load(string path)
        {
            var graph = new AnimatorGraph();

            if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject root)
                throw new InvalidDataException("Not an animator controller file.");

            graph.DefaultState = root["defaultState"]?.GetValue<string>() ?? string.Empty;

            if (root["parameters"] is JsonArray parameters)
            {
                foreach (var node in parameters)
                {
                    if (node is not JsonObject obj)
                        continue;

                    graph.Parameters.Add(new AnimatorParameterData
                    {
                        Name = obj["name"]?.GetValue<string>() ?? string.Empty,
                        Type = ParseEnum(obj["type"], AnimatorParameterType.Bool),
                    });
                }
            }

            if (root["states"] is JsonArray states)
            {
                foreach (var node in states)
                {
                    if (node is not JsonObject obj)
                        continue;

                    graph.States.Add(new AnimatorStateData
                    {
                        Name = obj["name"]?.GetValue<string>() ?? string.Empty,
                        Clip = obj["clip"]?.GetValue<string>() ?? string.Empty,
                        Position = new Vector2(GetFloat(obj["x"], 0f), GetFloat(obj["y"], 0f)),
                        Transitions = ParseTransitions(obj["transitions"]),
                    });
                }
            }

            graph.AnyTransitions = ParseTransitions(root["anyTransitions"]);
            return graph;
        }

        public static void Save(AnimatorGraph graph, string path)
        {
            var states = new JsonArray();

            foreach (var state in graph.States)
            {
                states.Add(new JsonObject
                {
                    ["name"] = state.Name,
                    ["clip"] = state.Clip ?? string.Empty,
                    ["x"] = state.Position.X,
                    ["y"] = state.Position.Y,
                    ["transitions"] = WriteTransitions(state.Transitions),
                });
            }

            var parameters = new JsonArray();

            foreach (var parameter in graph.Parameters)
            {
                parameters.Add(new JsonObject
                {
                    ["name"] = parameter.Name,
                    ["type"] = parameter.Type.ToString(),
                });
            }

            var root = new JsonObject
            {
                ["defaultState"] = graph.DefaultState ?? string.Empty,
                ["parameters"] = parameters,
                ["states"] = states,
                ["anyTransitions"] = WriteTransitions(graph.AnyTransitions),
            };

            File.WriteAllText(path, root.ToJsonString(_writeOptions));
        }

        private static List<AnimatorTransitionData> ParseTransitions(JsonNode node)
        {
            var result = new List<AnimatorTransitionData>();

            if (node is not JsonArray array)
                return result;

            foreach (var item in array)
            {
                if (item is not JsonObject obj)
                    continue;

                var transition = new AnimatorTransitionData
                {
                    To = obj["to"]?.GetValue<string>() ?? string.Empty,
                    HasExitTime = obj["hasExitTime"] is JsonValue exit && exit.TryGetValue(out bool e) && e,
                };

                if (obj["conditions"] is JsonArray conditions)
                {
                    foreach (var conditionNode in conditions)
                    {
                        if (conditionNode is not JsonObject conditionObj)
                            continue;

                        transition.Conditions.Add(new AnimatorConditionData
                        {
                            Parameter = conditionObj["parameter"]?.GetValue<string>() ?? string.Empty,
                            Mode = ParseEnum(conditionObj["mode"], AnimatorConditionMode.If),
                            Threshold = GetFloat(conditionObj["threshold"], 0f),
                        });
                    }
                }

                result.Add(transition);
            }

            return result;
        }

        private static JsonArray WriteTransitions(List<AnimatorTransitionData> transitions)
        {
            var result = new JsonArray();

            foreach (var transition in transitions)
            {
                var conditions = new JsonArray();

                foreach (var condition in transition.Conditions)
                {
                    conditions.Add(new JsonObject
                    {
                        ["parameter"] = condition.Parameter ?? string.Empty,
                        ["mode"] = condition.Mode.ToString(),
                        ["threshold"] = condition.Threshold,
                    });
                }

                result.Add(new JsonObject
                {
                    ["to"] = transition.To ?? string.Empty,
                    ["hasExitTime"] = transition.HasExitTime,
                    ["conditions"] = conditions,
                });
            }

            return result;
        }

        private static float GetFloat(JsonNode node, float fallback)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? f : fallback;
        }

        private static T ParseEnum<T>(JsonNode node, T fallback) where T : struct
        {
            return node is JsonValue value && value.TryGetValue(out string text) &&
                   Enum.TryParse<T>(text, true, out var parsed)
                ? parsed
                : fallback;
        }
    }
}
