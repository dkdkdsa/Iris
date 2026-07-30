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

        public string Name => "출력 정리";

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
                    context.Log($"예약된 폴더 이름과 겹쳐 정리를 건너뜀: {directory}");
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
                context.Log($"출력에 복사된 느슨한 콘텐츠 {removed}개 항목 제거");

            return Task.FromResult(true);
        }
    }
}
