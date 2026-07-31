using Hexa.NET.ImGui;
using Iris.Debugging;
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

        private const string DeletePopupId = "삭제 확인###DeleteConfirmPopup";

        private string _deletePath;
        private bool _deleteIsDirectory;
        private bool _deletePopupPending;

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

            if (_pendingAction != null)
            {
                var action = _pendingAction;
                _pendingAction = null;
                action();
            }

            if (_deletePopupPending)
            {
                _deletePopupPending = false;
                ImGui.OpenPopup(DeletePopupId);
            }

            DrawDeleteConfirm(workspace);
        }

        private void DrawDeleteConfirm(EditorWorkspace workspace)
        {
            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.WorkPos + viewport.WorkSize * 0.5f, ImGuiCond.Appearing,
                new System.Numerics.Vector2(0.5f, 0.5f));

            if (!ImGui.BeginPopupModal(DeletePopupId))
                return;

            if (_deletePath == null)
            {
                ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
                return;
            }

            ImGui.Text($"'{Path.GetFileName(Path.TrimEndingDirectorySeparator(_deletePath))}' 을(를) 삭제할까요?");

            if (_deleteIsDirectory)
                ImGui.TextDisabled("폴더 안의 모든 파일이 함께 삭제됩니다.");

            ImGui.TextDisabled("이 작업은 되돌릴 수 없습니다.");
            ImGui.Separator();

            if (ImGui.Button("삭제", new System.Numerics.Vector2(120f, 0f)))
            {
                ApplyDelete(workspace);
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("취소", new System.Numerics.Vector2(120f, 0f)))
            {
                _deletePath = null;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        private void ApplyDelete(EditorWorkspace workspace)
        {
            string absolute = workspace.ToAbsolute(_deletePath);

            try
            {
                if (_deleteIsDirectory)
                {
                    if (Directory.Exists(absolute))
                        Directory.Delete(absolute, true);
                }
                else if (File.Exists(absolute))
                {
                    File.Delete(absolute);
                }

                _context.HandlePathDeleted(absolute, _deleteIsDirectory);
                workspace.Refresh();
                Debug.Log($"삭제됨: {_deletePath}");
            }
            catch (Exception ex)
            {
                Debug.LogException("삭제 실패", ex);
            }

            _deletePath = null;
        }

        private void RequestDelete(string relativePath, bool isDirectory)
        {
            _deletePath = relativePath;
            _deleteIsDirectory = isDirectory;
            _deletePopupPending = true;
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
                        Debug.LogException("에셋 생성 실패", ex);
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

                    if (ImGui.MenuItem("삭제"))
                        RequestDelete(dir, isDirectory: true);

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
                    if (asset.AssetType == typeof(Iris.Assets.ITexture) && ImGui.MenuItem("스프라이트 슬라이스"))
                        _context.RequestOpenSpriteSlicer(asset.Path);

                    if (ImGui.MenuItem("이름 변경"))
                        StartRename(asset.Path, isDirectory: false);

                    if (ImGui.MenuItem("삭제"))
                        RequestDelete(asset.Path, isDirectory: false);

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
                Debug.LogError($"이름 변경 실패: {error}");
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

            if (asset.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                asset.Path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase) ||
                asset.Path.EndsWith(".tile", StringComparison.OrdinalIgnoreCase) ||
                asset.Path.EndsWith(".sprite", StringComparison.OrdinalIgnoreCase) ||
                asset.Path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                ExternalEditor.OpenScript(workspace.ProjectFile, workspace.ToAbsolute(asset.Path));
                return;
            }

            if (asset.Path.EndsWith(".ui", StringComparison.OrdinalIgnoreCase))
                _context.RequestOpenUILayout(workspace.ToAbsolute(asset.Path));

            if (asset.Path.EndsWith(".controller", StringComparison.OrdinalIgnoreCase))
                _context.RequestOpenAnimator(workspace.ToAbsolute(asset.Path));
        }

        private void OpenScene(EditorWorkspace workspace, AssetEntry asset)
        {
            try
            {
                _context.LoadScene(workspace.ToAbsolute(asset.Path));
            }
            catch (Exception ex)
            {
                Debug.LogException("씬 열기 실패", ex);
            }
        }
    }
}
