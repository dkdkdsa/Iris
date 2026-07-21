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

            var config = LoadConfig();
            string scenePath = GetArg(args, "--scene") ?? config.StartScene;

            var engine = new Engine(new DefaultPlatform());

            engine.OnStart += () =>
            {
                onInit?.Invoke();

                var sceneSystem = SystemManager.Instance.GetSystem<SceneSystem>();

                if (scenePath == null)
                {
                    sceneSystem.LoadScene(new Scene());
                    return;
                }

                try
                {
                    sceneSystem.LoadScene(SceneLoader.Load(scenePath));
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

        //이거 외부로 빼기


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
                    config.StartScene = obj["startScene"]?.GetValue<string>() ?? config.StartScene;
                    config.DefaultWidth = (int)(obj["width"]?.GetValue<float>() ?? config.DefaultWidth);
                    config.DefaultHeight = (int)(obj["height"]?.GetValue<float>() ?? config.DefaultHeight);
                    config.Title = obj["title"]?.GetValue<string>() ?? config.Title;
                    config.Fullscreen = obj["fullscreen"]?.GetValue<bool>() ?? config.Fullscreen;
                    config.Resizable = obj["resizable"]?.GetValue<bool>() ?? config.Resizable;
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
