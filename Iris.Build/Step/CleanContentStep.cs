using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Iris.Build.Step
{
    public sealed class CleanContentStep : IBuildStep
    {
        private static readonly HashSet<string> _reservedDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            "runtimes",
        };

        public string Name => "Clean Output";

        public Task<bool> Run(BuildContext context)
        {
            var topDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rootFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in context.Content)
            {
                int slash = file.Key.IndexOf('/');

                if (slash > 0)
                    topDirectories.Add(file.Key.Substring(0, slash));
                else
                    rootFiles.Add(file.Key);
            }

            int removed = 0;

            foreach (var directory in topDirectories)
            {
                if (_reservedDirectories.Contains(directory))
                {
                    context.Log($"Skipping cleanup for reserved folder name: {directory}");
                    continue;
                }

                string path = Path.Combine(context.OutputDirectory, directory);

                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    removed++;
                }
            }

            foreach (var file in rootFiles)
            {
                string path = Path.Combine(context.OutputDirectory, file);

                if (File.Exists(path))
                {
                    File.Delete(path);
                    removed++;
                }
            }

            if (removed > 0)
                context.Log($"Removed {removed} loose content item(s) from output");

            return Task.FromResult(true);
        }
    }
}
