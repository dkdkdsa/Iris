using Iris.Core;
using Iris.Debugging;
using Iris.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace Iris.Assets
{
    internal class AnimatorControllerLoader : IAssetLoader
    {
        private static readonly DebugChannel _log = Debug.Channel("Iris");

        public IAsset LoadAsset(string path)
        {
            if (JsonNode.Parse(VirtualFileSystem.ReadAllText(path)) is not JsonObject root)
                throw new InvalidDataException($"Not an animator controller file: {path}");

            var parameters = new List<AnimatorParameter>();

            if (root["parameters"] is JsonArray parameterArray)
            {
                foreach (var node in parameterArray)
                {
                    if (node is not JsonObject obj)
                        continue;

                    string name = obj["name"]?.GetValue<string>();

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    parameters.Add(new AnimatorParameter
                    {
                        Name = name,
                        Type = ParseEnum(obj["type"], AnimatorParameterType.Bool),
                    });
                }
            }

            var states = new List<AnimatorControllerState>();

            if (root["states"] is JsonArray stateArray)
            {
                foreach (var node in stateArray)
                {
                    if (node is not JsonObject obj)
                        continue;

                    string name = obj["name"]?.GetValue<string>();

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    states.Add(new AnimatorControllerState
                    {
                        Name = name,
                        Clip = LoadClip(obj["clip"]?.GetValue<string>()),
                        Transitions = ParseTransitions(obj["transitions"]),
                    });
                }
            }

            return new AnimatorController
            {
                DefaultState = root["defaultState"]?.GetValue<string>(),
                Parameters = parameters,
                States = states,
                AnyTransitions = ParseTransitions(root["anyTransitions"]),
            };
        }

        private static List<AnimatorControllerTransition> ParseTransitions(JsonNode node)
        {
            var result = new List<AnimatorControllerTransition>();

            if (node is not JsonArray array)
                return result;

            foreach (var item in array)
            {
                if (item is not JsonObject obj)
                    continue;

                string to = obj["to"]?.GetValue<string>();

                if (string.IsNullOrWhiteSpace(to))
                    continue;

                result.Add(new AnimatorControllerTransition
                {
                    To = to,
                    HasExitTime = obj["hasExitTime"] is JsonValue exit && exit.TryGetValue(out bool e) && e,
                    Conditions = ParseConditions(obj["conditions"]),
                });
            }

            return result;
        }

        private static List<AnimatorCondition> ParseConditions(JsonNode node)
        {
            var result = new List<AnimatorCondition>();

            if (node is not JsonArray array)
                return result;

            foreach (var item in array)
            {
                if (item is not JsonObject obj)
                    continue;

                string parameter = obj["parameter"]?.GetValue<string>();

                if (string.IsNullOrWhiteSpace(parameter))
                    continue;

                result.Add(new AnimatorCondition
                {
                    Parameter = parameter,
                    Mode = ParseEnum(obj["mode"], AnimatorConditionMode.If),
                    Threshold = obj["threshold"] is JsonValue t && t.TryGetValue(out float f) ? f : 0f,
                });
            }

            return result;
        }

        private static SpriteAnimation LoadClip(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return null;

            string fullPath = Path.IsPathRooted(relativePath)
                ? relativePath
                : Path.Combine(SceneLoader.ContentRoot, relativePath);

            try
            {
                return AssetManager.Load<SpriteAnimation>(fullPath);
            }
            catch (Exception ex)
            {
                _log.LogExceptionOnce($"Failed to load animation clip ({relativePath})", ex);
                return null;
            }
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
