using Iris.Assets;
using IrisEditor.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace IrisEditor.Workspace
{
    internal class EditorWorkspace
    {
        private static readonly HashSet<string> _ignoredDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".vs", "bin", "obj",
        };

        private static readonly Dictionary<string, Type> _typesByExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = typeof(ITexture),
            [".jpg"] = typeof(ITexture),
            [".jpeg"] = typeof(ITexture),
            [".bmp"] = typeof(ITexture),
            [".tga"] = typeof(ITexture),
            [".gif"] = typeof(ITexture),
            [".wav"] = typeof(IAudioClip),
            [".mp3"] = typeof(IAudioClip),
            [".ttf"] = typeof(IFont),
            [".otf"] = typeof(IFont),
            [".ui"] = typeof(Iris.UI.IUILayout),
            [".anim"] = typeof(Iris.Core.AnimationClip),
            [".sprite"] = typeof(Iris.Core.Sprite),
            [".tile"] = typeof(Iris.Core.Tile),
            [".controller"] = typeof(Iris.Core.AnimatorController),
            [".prefab"] = typeof(Iris.Core.Prefab),
            [".scene"] = typeof(SceneData),
        };

        private List<AssetEntry> _assets = new();
        private List<string> _directories = new();

        public string RootPath { get; set; }

        public string ProjectFile { get; private set; }

        public IReadOnlyList<AssetEntry> Assets => _assets;

        public IReadOnlyList<string> Directories => _directories;

        public EditorWorkspace(string rootPath)
        {
            RootPath = rootPath;
            Refresh();
        }

        public string ToAbsolute(string relativePath)
        {
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(RootPath, relativePath));
        }

        public bool TryRename(string relativePath, string newName, bool isDirectory, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(newName))
            {
                error = "The name is empty.";
                return false;
            }

            if (newName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
            {
                error = "The name contains invalid characters.";
                return false;
            }

            string oldFull = ToAbsolute(relativePath);
            string newFull = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(oldFull), newName);

            if (string.Equals(oldFull, newFull, StringComparison.Ordinal))
                return true;

            bool caseOnly = string.Equals(oldFull, newFull, StringComparison.OrdinalIgnoreCase);

            try
            {
                if (isDirectory)
                {
                    if (!caseOnly && (Directory.Exists(newFull) || File.Exists(newFull)))
                    {
                        error = "An item with the same name already exists.";
                        return false;
                    }

                    if (caseOnly)
                    {
                        string temp = newFull + "~renaming";
                        Directory.Move(oldFull, temp);
                        Directory.Move(temp, newFull);
                    }
                    else
                    {
                        Directory.Move(oldFull, newFull);
                    }
                }
                else
                {
                    if (!caseOnly && (File.Exists(newFull) || Directory.Exists(newFull)))
                    {
                        error = "An item with the same name already exists.";
                        return false;
                    }

                    File.Move(oldFull, newFull);
                }

                Refresh();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryMove(string relativePath, string targetPrefix, bool isDirectory, out string newRelativePath, out string error)
        {
            newRelativePath = relativePath;
            error = null;

            string source = System.IO.Path.TrimEndingDirectorySeparator(ToAbsolute(relativePath));
            string targetDirectory = System.IO.Path.TrimEndingDirectorySeparator(
                System.IO.Path.GetFullPath(System.IO.Path.Combine(RootPath, targetPrefix ?? string.Empty)));

            string destination = System.IO.Path.Combine(targetDirectory, System.IO.Path.GetFileName(source));

            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                return true;

            if (isDirectory && IsSelfOrDescendant(source, targetDirectory))
            {
                error = "A folder cannot be moved into itself.";
                return false;
            }

            if (File.Exists(destination) || Directory.Exists(destination))
            {
                error = "An item with the same name already exists in the target folder.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(targetDirectory);

                if (isDirectory)
                    Directory.Move(source, destination);
                else
                    File.Move(source, destination);

                newRelativePath = System.IO.Path.GetRelativePath(RootPath, destination).Replace('\\', '/');
                Refresh();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool IsSelfOrDescendant(string folder, string candidate)
        {
            if (string.Equals(folder, candidate, StringComparison.OrdinalIgnoreCase))
                return true;

            return candidate.StartsWith(folder + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        public void Refresh()
        {
            _assets.Clear();
            _directories.Clear();
            ProjectFile = null;

            if (string.IsNullOrEmpty(RootPath) || !Directory.Exists(RootPath))
                return;

            var projects = Directory.GetFiles(RootPath, "*.csproj");
            ProjectFile = projects.Length > 0 ? projects[0] : null;

            ScanDirectory(RootPath);

            _assets.Sort((a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
            _directories.Sort(StringComparer.OrdinalIgnoreCase);
        }

        private void ScanDirectory(string directory)
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                _assets.Add(new AssetEntry
                {
                    Id = Guid.NewGuid(),
                    Path = System.IO.Path.GetRelativePath(RootPath, file).Replace('\\', '/'),
                    AssetType = _typesByExtension.GetValueOrDefault(System.IO.Path.GetExtension(file)),
                });
            }

            foreach (var sub in Directory.EnumerateDirectories(directory))
            {
                if (_ignoredDirectories.Contains(System.IO.Path.GetFileName(sub)))
                    continue;

                _directories.Add(System.IO.Path.GetRelativePath(RootPath, sub).Replace('\\', '/'));
                ScanDirectory(sub);
            }
        }
    }
}
