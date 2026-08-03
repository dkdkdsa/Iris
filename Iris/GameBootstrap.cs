using Iris.Core;
using Iris.Debugging;
using Iris.Files;
using Iris.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json.Nodes;

namespace Iris
{
    public static class GameBootstrap
    {
        private static FileLogSink _fileSink;

        public static void Run(string[] args, Action onInit = null)
        {
            args ??= Array.Empty<string>();

            string contentRoot = GetArg(args, "--content");
            if (!string.IsNullOrEmpty(contentRoot))
                SceneLoader.ContentRoot = Path.GetFullPath(contentRoot);

            string packagePath = Path.Combine(SceneLoader.ContentRoot, Files.Package.PackageFormat.DefaultFileName);
            int packageEntries = -1;

            if (File.Exists(packagePath))
            {
                var packageProvider = new PackageFileProvider(packagePath);
                VirtualFileSystem.InjectProvider(packageProvider);
                packageEntries = packageProvider.EntryCount;
            }
            else
            {
                VirtualFileSystem.InjectProvider(new LooseFileProvider(SceneLoader.ContentRoot));
            }

            var config = LoadConfig();
            ConfigureLogging(config);
            RegisterGameAssemblies();

            Diagnostics.Stats.Enabled = config.Stats;

            if (packageEntries >= 0)
                Debug.Log($"Mounted package: {Files.Package.PackageFormat.DefaultFileName} ({packageEntries} entries)");

            SceneManager.SetBuildScenes(config.BuildScenes);

            string sceneOverride = GetArg(args, "--scene");

            var engine = new Engine(new DefaultPlatform());

            engine.OnStart += () =>
            {
                onInit?.Invoke();

                var renderSystem = SystemManager.Instance.GetSystem<Rendering.RenderSystem>();

                if (renderSystem != null)
                    renderSystem.SortByTexture = config.BatchByTexture;

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
                    Debug.LogException("Failed to load startup scene", ex);
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
                vsync = config.VSync,
                targetFrameRate = config.TargetFrameRate,
            });

            _fileSink?.Dispose();
            _fileSink = null;
        }

        private static void RegisterGameAssemblies()
        {
            string directory = AppContext.BaseDirectory;

            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            foreach (var file in Directory.EnumerateFiles(directory, "*.dll"))
            {
                if (!ReferencesEngine(file))
                    continue;

                try
                {
                    SceneLoader.RegisterAssembly(System.Reflection.Assembly.LoadFrom(file));
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce($"Failed to load game assembly: {Path.GetFileName(file)}", ex);
                }
            }
        }

        private static bool ReferencesEngine(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var reader = new PEReader(stream);

                if (!reader.HasMetadata)
                    return false;

                var metadata = reader.GetMetadataReader();

                foreach (var handle in metadata.AssemblyReferences)
                {
                    if (metadata.GetString(metadata.GetAssemblyReference(handle).Name) == "Iris")
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static void ConfigureLogging(ProjectConfig config)
        {
            if (!config.LogToFile)
            {
                Debug.ClearSinks();
                Debug.MinLevel = LogLevel.Off;
                return;
            }

            foreach (var path in LogPathCandidates(config))
            {
                try
                {
                    _fileSink = new FileLogSink(path);
                    Debug.AddSink(_fileSink);
                    Debug.Log($"Log file: {_fileSink.FilePath}");
                    return;
                }
                catch
                {
                }
            }

            Debug.LogWarning("Could not open a log file; file logging is disabled for this run.");
        }

        private static IEnumerable<string> LogPathCandidates(ProjectConfig config)
        {
            yield return Path.Combine(AppContext.BaseDirectory, FileLogSink.DefaultFileName);

            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (!string.IsNullOrEmpty(local))
                yield return Path.Combine(local, SafeFolderName(config.Title), FileLogSink.DefaultFileName);
        }

        private static string SafeFolderName(string title)
        {
            var builder = new StringBuilder();
            var invalid = Path.GetInvalidFileNameChars();

            foreach (char c in title ?? string.Empty)
            {
                if (Array.IndexOf(invalid, c) < 0)
                    builder.Append(c);
            }

            return builder.Length > 0 ? builder.ToString() : "IrisGame";
        }

        private static ProjectConfig LoadConfig()
        {
            var config = new ProjectConfig();
            string path = Path.Combine(SceneLoader.ContentRoot, "project.json");

            if (!VirtualFileSystem.Exists(path))
                return config;

            try
            {
                if (JsonNode.Parse(VirtualFileSystem.ReadAllText(path)) is JsonObject obj)
                {
                    config.DefaultWidth = (int)(obj["width"]?.GetValue<float>() ?? config.DefaultWidth);
                    config.DefaultHeight = (int)(obj["height"]?.GetValue<float>() ?? config.DefaultHeight);
                    config.Title = obj["title"]?.GetValue<string>() ?? config.Title;
                    config.Fullscreen = obj["fullscreen"]?.GetValue<bool>() ?? config.Fullscreen;
                    config.Resizable = obj["resizable"]?.GetValue<bool>() ?? config.Resizable;
                    config.VSync = obj["vsync"]?.GetValue<bool>() ?? config.VSync;
                    config.TargetFrameRate = (int)(obj["targetFrameRate"]?.GetValue<float>() ?? config.TargetFrameRate);
                    config.LogToFile = obj["logToFile"]?.GetValue<bool>() ?? config.LogToFile;
                    config.BatchByTexture = obj["batchByTexture"]?.GetValue<bool>() ?? config.BatchByTexture;
                    config.Stats = obj["stats"]?.GetValue<bool>() ?? config.Stats;

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
                Debug.LogException("Failed to read project.json", ex);
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
