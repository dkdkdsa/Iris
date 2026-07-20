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
                width = config.Width,
                height = config.Height,
                title = config.Title,
            });
        }

        //이거 외부로 빼기
        private sealed class ProjectConfig
        {
            public string StartScene;
            public int Width = 1280;
            public int Height = 720;
            public string Title = "Iris";
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
                    config.StartScene = obj["startScene"]?.GetValue<string>() ?? config.StartScene;
                    config.Width = (int)(obj["width"]?.GetValue<float>() ?? config.Width);
                    config.Height = (int)(obj["height"]?.GetValue<float>() ?? config.Height);
                    config.Title = obj["title"]?.GetValue<string>() ?? config.Title;
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
