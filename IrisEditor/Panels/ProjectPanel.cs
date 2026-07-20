using Hexa.NET.ImGui;
using IrisEditor.Data;
using IrisEditor.Platform;
using IrisEditor.Workspace;
using System;
using System.IO;

namespace IrisEditor.Panels
{
    internal sealed class ProjectPanel : EditorPanel
    {
        private readonly EditorContext _context;
        private Action _pendingAction;

        private string _renamingPath;
        private bool _renamingIsDirectory;
        private string _renameBuffer = string.Empty;
        private bool _renameFocusPending;

        public ProjectPanel(EditorContext context)
        {
            _context = context;
        }

        public override string Title => "프로젝트";

        protected override void OnGui()
        {
            var workspace = _context.Workspace;

            if (workspace == null)
            {
                ImGui.TextDisabled("열린 프로젝트가 없습니다");
                ImGui.Spacing();

                if (ImGui.Button("프로젝트 열기"))
                    _context.OpenProjectWithDialog();

                if (ImGui.Button("새로운 프로젝트 생성"))
                    _context.CreateProjectWithDialog();

                return;
            }

            ImGui.Text(Path.GetFileName(Path.TrimEndingDirectorySeparator(workspace.RootPath)));
            ImGui.SameLine();

            if (ImGui.SmallButton("새로고침"))
                workspace.Refresh();

            ImGui.Separator();

            ImGui.BeginChild("AssetTree");
            DrawDirectory(workspace, string.Empty);

            if (ImGui.BeginPopupContextWindow("ProjectContext", ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
            {
                DrawCreateMenu(workspace, string.Empty);
                ImGui.EndPopup();
            }

            ImGui.EndChild();

            // 생성은 트리 순회 밖에서 실행한다 — Refresh()가 순회 중인 목록을 수정하면 터진다.
            if (_pendingAction != null)
            {
                var action = _pendingAction;
                _pendingAction = null;
                action();
            }
        }

        private void DrawCreateMenu(EditorWorkspace workspace, string prefix)
        {
            if (!ImGui.BeginMenu("생성"))
                return;

            foreach (var creator in AssetCreatorRegistry.Creators)
            {
                if (!ImGui.MenuItem(creator.MenuName))
                    continue;

                var picked = creator;

                _pendingAction = () =>
                {
                    try
                    {
                        string directory = Path.Combine(workspace.RootPath, prefix);
                        string path = AssetCreatorRegistry.MakeUniquePath(directory, picked.DefaultFileName);

                        picked.Create(path);
                        workspace.Refresh();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[에디터] 에셋 생성 실패: {ex.Message}");
                    }
                };
            }

            ImGui.EndMenu();
        }

        private void DrawDirectory(EditorWorkspace workspace, string prefix)
        {
            foreach (var dir in workspace.Directories)
            {
                if (!dir.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string rest = dir.Substring(prefix.Length);
                if (rest.Length == 0 || rest.Contains('/'))
                    continue;

                if (_renamingPath == dir && _renamingIsDirectory)
                {
                    DrawRenameInput(workspace, dir, isDirectory: true);
                    continue;
                }

                bool nodeOpen = ImGui.TreeNode(rest);

                if (ImGui.BeginPopupContextItem())
                {
                    DrawCreateMenu(workspace, prefix + rest + "/");

                    ImGui.Separator();

                    if (ImGui.MenuItem("이름 변경"))
                        StartRename(dir, isDirectory: true);

                    ImGui.EndPopup();
                }

                if (nodeOpen)
                {
                    DrawDirectory(workspace, prefix + rest + "/");
                    ImGui.TreePop();
                }
            }

            foreach (var asset in workspace.Assets)
            {
                if (!asset.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string rest = asset.Path.Substring(prefix.Length);
                if (rest.Contains('/'))
                    continue;

                if (_renamingPath == asset.Path && !_renamingIsDirectory)
                {
                    DrawRenameInput(workspace, asset.Path, isDirectory: false);
                    continue;
                }

                bool known = asset.AssetType != null;

                if (!known)
                    ImGui.PushStyleColor(ImGuiCol.Text, 0xFF808080);

                ImGui.Selectable($"{rest}##{asset.Id}");

                if (!known)
                    ImGui.PopStyleColor();

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    HandleDoubleClick(workspace, asset);

                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("이름 변경"))
                        StartRename(asset.Path, isDirectory: false);

                    ImGui.EndPopup();
                }

                if (known && ImGui.BeginDragDropSource())
                {
                    AssetDragDrop.Current = asset;

                    unsafe
                    {
                        byte dummy = 0;
                        ImGui.SetDragDropPayload(AssetDragDrop.PayloadType, &dummy, 1);
                    }

                    ImGui.Text(rest);
                    ImGui.EndDragDropSource();
                }
            }
        }

        private void StartRename(string relativePath, bool isDirectory)
        {
            _renamingPath = relativePath;
            _renamingIsDirectory = isDirectory;
            _renameBuffer = Path.GetFileName(relativePath);
            _renameFocusPending = true;
        }

        private void DrawRenameInput(EditorWorkspace workspace, string relativePath, bool isDirectory)
        {
            if (_renameFocusPending)
            {
                ImGui.SetKeyboardFocusHere();
                _renameFocusPending = false;
            }

            ImGui.SetNextItemWidth(-1f);

            bool commit = ImGui.InputText($"##Rename{relativePath}", ref _renameBuffer, 128,
                ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll);

            if (commit)
            {
                string target = relativePath;
                string newName = _renameBuffer;
                bool isDir = isDirectory;

                // 순회 밖에서 실행 - Refresh가 순회 중인 목록을 바꾸면 터진다.
                _pendingAction = () => ApplyRename(workspace, target, newName, isDir);
                _renamingPath = null;
                return;
            }

            if (ImGui.IsItemDeactivated())
                _renamingPath = null;
        }

        private void ApplyRename(EditorWorkspace workspace, string relativePath, string newName, bool isDirectory)
        {
            string oldAbsolute = workspace.ToAbsolute(relativePath);

            if (!workspace.TryRename(relativePath, newName, isDirectory, out var error))
            {
                Console.WriteLine($"[에디터] 이름 변경 실패: {error}");
                return;
            }

            string newAbsolute = Path.Combine(Path.GetDirectoryName(oldAbsolute), newName);
            _context.HandlePathRenamed(oldAbsolute, newAbsolute, isDirectory);
        }

        private void HandleDoubleClick(EditorWorkspace workspace, AssetEntry asset)
        {
            if (asset.AssetType == typeof(SceneData))
            {
                OpenScene(workspace, asset);
                return;
            }

            if (asset.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                ExternalEditor.OpenScript(workspace.ProjectFile, workspace.ToAbsolute(asset.Path));
        }

        private void OpenScene(EditorWorkspace workspace, AssetEntry asset)
        {
            try
            {
                _context.LoadScene(workspace.ToAbsolute(asset.Path));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[에디터] 씬 열기 실패: {ex.Message}");
            }
        }
    }
}
