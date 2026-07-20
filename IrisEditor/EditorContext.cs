using Iris.Core;
using IrisEditor.Data;
using IrisEditor.Platform;
using IrisEditor.Serialization;
using IrisEditor.Workspace;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

        public void OpenWorkspace(string rootPath)
        {
            Workspace = new EditorWorkspace(rootPath);
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
                    if (resolved != null)
                        comp.TargetType = resolved;
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

        public ActorData CreateActor()
        {
            var actor = new ActorData
            {
                Id = Guid.NewGuid(),
                Name = UniqueName("Actor"),
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
            Scene.Actors.Remove(actor);
            if (Selected == actor)
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
