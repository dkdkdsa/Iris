using Iris.Core;
using Iris.Platform;
using System;
using System.IO;
using System.Text.Json.Nodes;

namespace Iris
{
    public static class GameBootstrap
    {
        public static void Run(string[] args, Action onInit = null)
        {
            args ??= Array.Empty<string>();

            string contentRoot = GetArg(args, "--content");
            if (!string.IsNullOrEmpty(contentRoot))
                SceneLoader.ContentRoot = Path.GetFullPath(contentRoot);

            var config = LoadConfig();
            SceneManager.SetBuildScenes(config.BuildScenes);

            string sceneOverride = GetArg(args, "--scene");

            var engine = new Engine(new DefaultPlatform());

            engine.OnStart += () =>
            {
                onInit?.Invoke();

                var sceneSystem = SystemManager.Instance.GetSystem<SceneSystem>();

                try
                {
                    if (sceneOverride != null)
                        SceneManager.LoadFromPath(sceneOverride);
                    else if (config.BuildScenes.Count > 0)
                        SceneManager.LoadScene(0);
                    else
                        sceneSystem.LoadScene(new Scene());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    sceneSystem.LoadScene(new Scene());
                }
            };

            engine.Run(new WindowConfig
            {
                width = config.DefaultWidth,
                height = config.DefaultHeight,
                title = config.Title,
                resizable = config.Resizable,
                fullscreen = config.Fullscreen,
            });
        }


        private static ProjectConfig LoadConfig()
        {
            var config = new ProjectConfig();
            string path = Path.Combine(SceneLoader.ContentRoot, "project.json");

            if (!File.Exists(path))
                return config;

            try
            {
                if (JsonNode.Parse(File.ReadAllText(path)) is JsonObject obj)
                {
                    config.DefaultWidth = (int)(obj["width"]?.GetValue<float>() ?? config.DefaultWidth);
                    config.DefaultHeight = (int)(obj["height"]?.GetValue<float>() ?? config.DefaultHeight);
                    config.Title = obj["title"]?.GetValue<string>() ?? config.Title;
                    config.Fullscreen = obj["fullscreen"]?.GetValue<bool>() ?? config.Fullscreen;
                    config.Resizable = obj["resizable"]?.GetValue<bool>() ?? config.Resizable;

                    if (obj["buildScenes"] is JsonArray buildScenes)
                    {
                        foreach (var node in buildScenes)
                        {
                            if (node?.GetValue<string>() is string scene && !string.IsNullOrWhiteSpace(scene))
                                config.BuildScenes.Add(scene);
                        }
                    }

                    if (config.BuildScenes.Count == 0 && obj["startScene"]?.GetValue<string>() is string legacy &&
                        !string.IsNullOrWhiteSpace(legacy))
                        config.BuildScenes.Add(legacy);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Iris] project.json 읽기 실패: {ex.Message}");
            }

            return config;
        }

        private static string GetArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }

            return null;
        }
    }
}
