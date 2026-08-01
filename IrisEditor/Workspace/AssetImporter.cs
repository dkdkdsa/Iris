using System;
using System.IO;

namespace IrisEditor.Workspace
{
    internal static class AssetImporter
    {
        public static bool TryImport(EditorWorkspace workspace, string sourcePath, string targetPrefix, out string imported, out string error)
        {
            imported = null;
            error = null;

            if (workspace == null)
            {
                error = "No project is open.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                error = "The source path is empty.";
                return false;
            }

            string source;

            try
            {
                source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourcePath));
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            bool isDirectory = Directory.Exists(source);

            if (!isDirectory && !File.Exists(source))
            {
                error = "The dropped item no longer exists.";
                return false;
            }

            string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(workspace.RootPath));

            if (IsInside(source, root))
            {
                error = "The item is already in the project.";
                return false;
            }

            string targetDirectory = Path.Combine(root, targetPrefix ?? string.Empty);

            try
            {
                Directory.CreateDirectory(targetDirectory);

                string destination = AssetCreatorRegistry.MakeUniquePath(targetDirectory, Path.GetFileName(source));

                if (isDirectory)
                    CopyDirectory(source, destination);
                else
                    File.Copy(source, destination, false);

                imported = Path.GetRelativePath(root, destination).Replace('\\', '/');
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool IsInside(string path, string root)
        {
            if (string.Equals(path, root, StringComparison.OrdinalIgnoreCase))
                return true;

            return path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.EnumerateFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);

            foreach (var directory in Directory.EnumerateDirectories(source))
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}
