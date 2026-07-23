using Iris.Core;
using IrisEditor.Data;
using IrisEditor.Platform;
using IrisEditor.Serialization;
using IrisEditor.Workspace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;

namespace IrisEditor
{
    internal sealed class EditorContext
    {
        public SceneData Scene { get; private set; } = new();
        public ActorData Selected;
        public string ScenePath { get; private set; }
        public EditorWorkspace Workspace { get; private set; }
        public GameAssemblyHost Scripts { get; } = new();
        public GameBuilder Builder { get; } = new();
        public bool Dirty { get; private set; }

        public void MarkDirty()
        {
            Dirty = true;
        }

        public string PendingUILayoutPath { get; private set; }

        public void RequestOpenUILayout(string absolutePath)
        {
            PendingUILayoutPath = absolutePath;
        }

        public string ConsumePendingUILayout()
        {
            var path = PendingUILayoutPath;
            PendingUILayoutPath = null;
            return path;
        }

        public void OpenWorkspace(string rootPath)
        {
            Workspace = new EditorWorkspace(rootPath);

            SceneLoader.ContentRoot = rootPath;

            RefreshScripts();
        }

        public void OpenProjectWithDialog()
        {
            FileDialog.OpenFolder(path =>
            {
                if (path != null)
                    OpenWorkspace(path);
            });
        }

        public void CreateProjectWithDialog()
        {
            FileDialog.OpenFolder(path =>
            {
                if (path == null)
                    return;

                if (!ProjectScaffolder.TryCreate(path, out var error))
                {
                    Console.WriteLine($"[에디터] 프로젝트 생성 실패: {error}");
                    return;
                }

                OpenWorkspace(path);

                try
                {
                    LoadScene(Path.Combine(path, "Scenes", "Main.scene"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[에디터] 씬 열기 실패: {ex.Message}");
                }
            });
        }

        public void HandlePathRenamed(string oldPath, string newPath, bool isDirectory)
        {
            if (ScenePath == null)
                return;

            string fullScene = Path.GetFullPath(ScenePath);
            string fullOld = Path.GetFullPath(oldPath);

            if (!isDirectory)
            {
                if (string.Equals(fullScene, fullOld, StringComparison.OrdinalIgnoreCase))
                    ScenePath = newPath;

                return;
            }

            string oldPrefix = fullOld + Path.DirectorySeparatorChar;

            if (fullScene.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                ScenePath = Path.Combine(newPath, fullScene.Substring(oldPrefix.Length));
        }

        public void RunGame()
        {
            if (Workspace?.ProjectFile == null)
            {
                Console.WriteLine("[에디터] 실행할 프로젝트가 없습니다. 프로젝트를 먼저 여세요.");
                return;
            }

            if (Scripts.Building || Builder.Building)
            {
                Console.WriteLine("[에디터] 빌드 중에는 실행할 수 없습니다.");
                return;
            }

            if (Dirty && ScenePath != null)
            {
                try
                {
                    SaveScene(ScenePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[에디터] 씬 저장 실패: {ex.Message}");
                    return;
                }
            }

            string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Workspace.RootPath));
            string sceneArg = string.Empty;

            if (ScenePath != null)
            {
                string fullScene = Path.GetFullPath(ScenePath);
                string relative = Path.GetRelativePath(root, fullScene);
                string scene = relative.StartsWith("..", StringComparison.Ordinal)
                    ? fullScene
                    : relative.Replace('\\', '/');

                sceneArg = $" --scene \"{scene}\"";
            }

            var info = new ProcessStartInfo("dotnet",
                $"run --project \"{Workspace.ProjectFile}\" -- --content \"{root}\"{sceneArg}")
            {
                UseShellExecute = true,
                WorkingDirectory = root,
            };

            try
            {
                Process.Start(info);
                Console.WriteLine("[에디터] 게임을 실행합니다...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[에디터] 게임 실행 실패: {ex.Message}");
            }
        }

        public void HandlePathDeleted(string absolutePath, bool isDirectory)
        {
            if (ScenePath == null)
                return;

            string fullScene = Path.GetFullPath(ScenePath);
            string fullDeleted = Path.GetFullPath(absolutePath);

            bool affected = isDirectory
                ? fullScene.StartsWith(fullDeleted + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                : string.Equals(fullScene, fullDeleted, StringComparison.OrdinalIgnoreCase);

            if (affected)
            {
                ScenePath = null;
                MarkDirty();
            }
        }

        public void BuildGameWithDialog()
        {
            if (Workspace?.ProjectFile == null)
            {
                Console.WriteLine("[에디터] 빌드할 프로젝트가 없습니다. 프로젝트를 먼저 여세요.");
                return;
            }

            if (Builder.Building)
                return;

            if (Dirty)
                Console.WriteLine("[에디터] 저장 안 된 씬 변경사항이 있습니다. 빌드에는 저장된 파일이 들어갑니다.");

            FileDialog.OpenFolder(path =>
            {
                if (path == null)
                    return;

                string fullOut = Path.GetFullPath(path);
                string fullRoot = Path.GetFullPath(Workspace.RootPath);

                if (string.Equals(fullOut, fullRoot, StringComparison.OrdinalIgnoreCase) ||
                    fullOut.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[에디터] 출력 폴더는 프로젝트 폴더 밖이어야 합니다.");
                    return;
                }

                Builder.Build(Workspace.ProjectFile, fullOut, success =>
                {
                    if (!success)
                        return;

                    CopyBuildContent(fullOut);
                    Console.WriteLine($"[에디터] 빌드 완료: {fullOut}");

                    try
                    {
                        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{fullOut}\"") { UseShellExecute = true });
                    }
                    catch
                    {
                    }
                });
            });
        }

        private void CopyBuildContent(string outputDirectory)
        {
            Workspace.Refresh();

            foreach (var asset in Workspace.Assets)
            {
                bool include = asset.AssetType != null ||
                               string.Equals(asset.Path, "project.json", StringComparison.OrdinalIgnoreCase);

                if (!include)
                    continue;

                try
                {
                    string source = Workspace.ToAbsolute(asset.Path);
                    string target = Path.Combine(outputDirectory, asset.Path);

                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(source, target, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[에디터] 콘텐츠 복사 실패({asset.Path}): {ex.Message}");
                }
            }
        }

        public void RefreshScripts()
        {
            if (Workspace?.ProjectFile == null)
                return;

            Scripts.BuildAndLoad(Workspace.ProjectFile, success =>
            {
                if (!success)
                    return;

                ComponentCatalog.SetGameAssembly(Scripts.Current);
                UIObjectCatalog.SetGameAssembly(Scripts.Current);
                ReResolveComponents();
            });
        }

        private void ReResolveComponents()
        {
            foreach (var actor in Scene.Actors)
            {
                foreach (var comp in actor.Components)
                {
                    if (comp.TypeName == null)
                        continue;

                    var resolved = ComponentCatalog.Resolve(comp.TypeName);
                    if (resolved == null)
                        continue;

                    comp.TargetType = resolved;
                    ComponentCatalog.ApplyMissingDefaults(comp.Properties as JsonObject, resolved);
                }
            }
        }

        public void LoadScene(string path)
        {
            Scene = SceneSerializer.Load(path);
            Selected = null;
            ScenePath = path;
            Dirty = false;
        }

        public void SaveScene(string path)
        {
            SceneSerializer.Save(Scene, path);
            ScenePath = path;
            Dirty = false;
        }

        public void SaveAsPrefab(ActorData actor)
        {
            if (Workspace == null)
            {
                Console.WriteLine("[에디터] 프로젝트가 없어 프리팹을 저장할 수 없습니다.");
                return;
            }

            try
            {
                var builder = new StringBuilder();
                var invalid = Path.GetInvalidFileNameChars();

                foreach (char c in actor.Name ?? string.Empty)
                {
                    if (Array.IndexOf(invalid, c) < 0)
                        builder.Append(c);
                }

                string name = builder.Length > 0 ? builder.ToString() : "Prefab";
                string path = AssetCreatorRegistry.MakeUniquePath(Workspace.RootPath, $"{name}.prefab");

                SceneSerializer.SavePrefab(Scene, actor, path);
                Workspace.Refresh();
                Console.WriteLine($"[에디터] 프리팹 저장: {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[에디터] 프리팹 저장 실패: {ex.Message}");
            }
        }

        public void InstantiatePrefab(string relativePath, Vector2 worldPosition)
        {
            if (Workspace == null)
                return;

            try
            {
                string fullPath = Workspace.ToAbsolute(relativePath);

                if (JsonNode.Parse(File.ReadAllText(fullPath)) is not JsonObject root ||
                    root["actors"] is not JsonArray actorsJson)
                {
                    Console.WriteLine($"[에디터] 프리팹 파일 형식이 아닙니다: {relativePath}");
                    return;
                }

                var actors = SceneSerializer.ParseActors(actorsJson);

                if (actors.Count == 0)
                    return;

                var idMap = new Dictionary<Guid, Guid>();

                foreach (var actor in actors)
                {
                    var newId = Guid.NewGuid();
                    idMap[actor.Id] = newId;
                    actor.Id = newId;
                }

                ActorData rootActor = null;

                foreach (var actor in actors)
                {
                    foreach (var comp in actor.Components)
                        comp.Id = Guid.NewGuid();

                    if (actor.ParentId is Guid parentId && idMap.TryGetValue(parentId, out var mapped))
                    {
                        actor.ParentId = mapped;
                    }
                    else
                    {
                        actor.ParentId = null;
                        rootActor ??= actor;
                    }
                }

                if (rootActor != null)
                {
                    rootActor.PrefabSource = relativePath;
                    rootActor.GetComponent(typeof(Transform))?.SetVector2("LocalPosition", worldPosition);
                }

                Scene.Actors.AddRange(actors);
                Selected = rootActor ?? actors[0];
                MarkDirty();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[에디터] 프리팹 배치 실패: {ex.Message}");
            }
        }

        public ActorData CreateActor(ActorData parent = null)
        {
            var actor = new ActorData
            {
                Id = Guid.NewGuid(),
                Name = UniqueName("Actor"),
                ParentId = parent?.Id,
            };
            AddComponent(actor, typeof(Transform));

            Scene.Actors.Add(actor);
            Selected = actor;
            MarkDirty();
            return actor;
        }

        public ComponentData AddComponent(ActorData actor, Type type)
        {
            var comp = new ComponentData
            {
                Id = Guid.NewGuid(),
                TargetType = type,
                TypeName = type.FullName,
                Properties = ComponentCatalog.DefaultProperties(type),
            };

            actor.Components.Add(comp);
            MarkDirty();
            return comp;
        }

        public void RemoveComponent(ActorData actor, ComponentData comp)
        {
            actor.Components.Remove(comp);
            MarkDirty();
        }

        public void RemoveActor(ActorData actor)
        {
            var removed = new List<ActorData>();
            var stack = new Stack<ActorData>();
            stack.Push(actor);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                removed.Add(current);

                foreach (var candidate in Scene.Actors)
                {
                    if (candidate.ParentId == current.Id && !removed.Contains(candidate))
                        stack.Push(candidate);
                }
            }

            foreach (var item in removed)
                Scene.Actors.Remove(item);

            if (removed.Contains(Selected))
                Selected = null;

            MarkDirty();
        }

        private string UniqueName(string baseName)
        {
            if (Scene.Actors.All(x => x.Name != baseName))
                return baseName;

            int index = 1;
            while (Scene.Actors.Any(x => x.Name == $"{baseName} ({index})"))
                index++;

            return $"{baseName} ({index})";
        }
    }
}
