using System;
using System.Collections.Generic;
using System.IO;

namespace Iris.Core
{
    public static class SceneManager
    {
        private static readonly List<string> _buildScenes = new();

        public static Scene Active => SystemManager.Instance.GetSystem<SceneSystem>().ActiveScene;

        public static IReadOnlyList<string> BuildScenes => _buildScenes;

        public static void SetBuildScenes(IEnumerable<string> scenePaths)
        {
            _buildScenes.Clear();

            if (scenePaths == null)
                return;

            foreach (var path in scenePaths)
            {
                if (!string.IsNullOrWhiteSpace(path))
                    _buildScenes.Add(path.Replace('\\', '/'));
            }
        }

        public static Scene LoadScene(string name)
        {
            string path = ResolveScenePath(name);

            if (path == null)
            {
                Console.WriteLine($"[Iris] 빌드 씬 목록에 '{name}'이(가) 없습니다.");
                return null;
            }

            return LoadFromPath(path);
        }

        public static Scene LoadScene(int buildIndex)
        {
            if (buildIndex < 0 || buildIndex >= _buildScenes.Count)
            {
                Console.WriteLine($"[Iris] 빌드 씬 인덱스가 범위를 벗어남: {buildIndex} (0~{_buildScenes.Count - 1})");
                return null;
            }

            return LoadFromPath(_buildScenes[buildIndex]);
        }

        public static void LoadScene(Scene scene)
        {
            SystemManager.Instance.GetSystem<SceneSystem>().LoadScene(scene);
        }

        internal static Scene LoadFromPath(string path)
        {
            var scene = SceneLoader.Load(path);
            SystemManager.Instance.GetSystem<SceneSystem>().LoadScene(scene);
            return scene;
        }

        private static string ResolveScenePath(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            foreach (var path in _buildScenes)
            {
                if (string.Equals(Path.GetFileNameWithoutExtension(path), name, StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            string normalized = name.Replace('\\', '/');

            foreach (var path in _buildScenes)
            {
                if (string.Equals(path, normalized, StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return null;
        }
    }
}
