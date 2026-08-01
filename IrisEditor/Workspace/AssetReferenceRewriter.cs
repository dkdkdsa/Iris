using Iris.Debugging;
using IrisEditor.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IrisEditor.Workspace
{
    internal static class AssetReferenceRewriter
    {
        private static readonly HashSet<string> _documentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".scene", ".prefab", ".sprite", ".tile", ".anim", ".controller", ".ui", ".json",
        };

        private static readonly JsonSerializerOptions _writeOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static int RewriteProject(EditorWorkspace workspace, string oldRelativePath, string newRelativePath, bool isDirectory)
        {
            if (workspace == null || !TryPrepare(oldRelativePath, newRelativePath, out var oldPath, out var newPath))
                return 0;

            int changedFiles = 0;

            foreach (var asset in workspace.Assets)
            {
                if (!_documentExtensions.Contains(Path.GetExtension(asset.Path)))
                    continue;

                if (RewriteFile(workspace.ToAbsolute(asset.Path), oldPath, newPath, isDirectory))
                    changedFiles++;
            }

            return changedFiles;
        }

        public static int RewriteScene(SceneData scene, string oldRelativePath, string newRelativePath, bool isDirectory)
        {
            if (scene == null || !TryPrepare(oldRelativePath, newRelativePath, out var oldPath, out var newPath))
                return 0;

            int changed = 0;

            foreach (var actor in scene.Actors)
            {
                if (TryMap(actor.PrefabSource, oldPath, newPath, isDirectory, out var mapped))
                {
                    actor.PrefabSource = mapped;
                    changed++;
                }

                foreach (var component in actor.Components)
                {
                    if (component.Properties != null)
                        changed += RewriteNode(component.Properties, oldPath, newPath, isDirectory);
                }
            }

            return changed;
        }

        private static bool TryPrepare(string oldRelativePath, string newRelativePath, out string oldPath, out string newPath)
        {
            oldPath = Normalize(oldRelativePath);
            newPath = Normalize(newRelativePath);

            return oldPath.Length > 0 && newPath.Length > 0 &&
                   !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool RewriteFile(string absolutePath, string oldPath, string newPath, bool isDirectory)
        {
            try
            {
                if (!File.Exists(absolutePath))
                    return false;

                if (JsonNode.Parse(File.ReadAllText(absolutePath)) is not JsonNode root)
                    return false;

                if (RewriteNode(root, oldPath, newPath, isDirectory) == 0)
                    return false;

                File.WriteAllText(absolutePath, root.ToJsonString(_writeOptions));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException($"Failed to update references in {Path.GetFileName(absolutePath)}", ex);
                return false;
            }
        }

        private static int RewriteNode(JsonNode node, string oldPath, string newPath, bool isDirectory)
        {
            if (node is JsonObject obj)
                return RewriteObject(obj, oldPath, newPath, isDirectory);

            if (node is JsonArray array)
                return RewriteArray(array, oldPath, newPath, isDirectory);

            return 0;
        }

        private static int RewriteObject(JsonObject obj, string oldPath, string newPath, bool isDirectory)
        {
            List<KeyValuePair<string, string>> updates = null;
            int changed = 0;

            foreach (var pair in obj)
            {
                if (pair.Value is JsonValue value)
                {
                    if (value.TryGetValue(out string text) && TryMap(text, oldPath, newPath, isDirectory, out var mapped))
                        (updates ??= new List<KeyValuePair<string, string>>()).Add(new(pair.Key, mapped));
                }
                else if (pair.Value != null)
                {
                    changed += RewriteNode(pair.Value, oldPath, newPath, isDirectory);
                }
            }

            if (updates == null)
                return changed;

            foreach (var update in updates)
                obj[update.Key] = JsonValue.Create(update.Value);

            return changed + updates.Count;
        }

        private static int RewriteArray(JsonArray array, string oldPath, string newPath, bool isDirectory)
        {
            int changed = 0;

            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] is JsonValue value)
                {
                    if (value.TryGetValue(out string text) && TryMap(text, oldPath, newPath, isDirectory, out var mapped))
                    {
                        array[i] = JsonValue.Create(mapped);
                        changed++;
                    }
                }
                else if (array[i] != null)
                {
                    changed += RewriteNode(array[i], oldPath, newPath, isDirectory);
                }
            }

            return changed;
        }

        private static bool TryMap(string value, string oldPath, string newPath, bool isDirectory, out string mapped)
        {
            mapped = null;

            if (string.IsNullOrEmpty(value))
                return false;

            string normalized = value.Replace('\\', '/');

            if (string.Equals(normalized, oldPath, StringComparison.OrdinalIgnoreCase))
            {
                mapped = newPath;
                return true;
            }

            if (!isDirectory || !normalized.StartsWith(oldPath + "/", StringComparison.OrdinalIgnoreCase))
                return false;

            mapped = newPath + normalized.Substring(oldPath.Length);
            return true;
        }

        private static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return path.Replace('\\', '/').Trim().TrimEnd('/');
        }
    }
}
