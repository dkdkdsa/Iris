using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Iris.Build.Step
{
    public sealed class CollectContentStep : IBuildStep
    {
        public static readonly HashSet<string> ContentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif",
            ".wav", ".mp3",
            ".ttf", ".otf",
            ".ui", ".anim", ".sprite", ".tile", ".controller", ".prefab", ".scene",
        };

        private static readonly HashSet<string> _ignoredDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".vs", "bin", "obj",
        };

        public string Name => "콘텐츠 수집";

        public Task<bool> Run(BuildContext context)
        {
            context.Content.Clear();

            if (string.IsNullOrEmpty(context.ContentRoot) || !Directory.Exists(context.ContentRoot))
            {
                context.Log($"콘텐츠 루트가 없습니다: {context.ContentRoot}");
                return Task.FromResult(false);
            }

            Scan(context, context.ContentRoot);

            string projectJson = Path.Combine(context.ContentRoot, "project.json");

            if (File.Exists(projectJson))
                context.Content.Add(new ContentFile("project.json", projectJson));

            context.Log($"에셋 {context.Content.Count}개 수집");
            return Task.FromResult(true);
        }

        private static void Scan(BuildContext context, string directory)
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                if (!ContentExtensions.Contains(Path.GetExtension(file)))
                    continue;

                string key = Path.GetRelativePath(context.ContentRoot, file).Replace('\\', '/');
                context.Content.Add(new ContentFile(key, file));
            }

            foreach (var sub in Directory.EnumerateDirectories(directory))
            {
                if (_ignoredDirectories.Contains(Path.GetFileName(sub)))
                    continue;

                if (IsOutputDirectory(context, sub))
                    continue;

                Scan(context, sub);
            }
        }

        private static bool IsOutputDirectory(BuildContext context, string directory)
        {
            if (string.IsNullOrEmpty(context.OutputDirectory))
                return false;

            return string.Equals(Path.GetFullPath(directory), Path.GetFullPath(context.OutputDirectory),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
