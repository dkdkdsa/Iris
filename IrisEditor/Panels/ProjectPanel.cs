using Hexa.NET.ImGui;
using Iris.Debugging;
using IrisEditor.Data;
using IrisEditor.Platform;
using IrisEditor.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using IrisEditor.Localization;

namespace IrisEditor.Panels
{
    internal sealed class ProjectPanel : EditorPanel
    {
        private readonly EditorContext _context;
        private readonly List<(Vector2 Min, Vector2 Max, string Prefix)> _folderRects = new();
        private Action _pendingAction;

        private string _renamingPath;
        private bool _renamingIsDirectory;
        private string _renameBuffer = string.Empty;
        private bool _renameFocusPending;

        private static string DeletePopupId => Loc.Window("project.deletePopup");

        private string _deletePath;
        private bool _deleteIsDirectory;
        private bool _deletePopupPending;

        public ProjectPanel(EditorContext context)
        {
            _context = context;
        }

        public override string Title => Loc.Window("panel.project");

        protected override void OnGui()
        {
            var workspace = _context.Workspace;

            if (workspace == null)
            {
                ImGui.TextDisabled(Loc.T("common.noProject"));
                ImGui.Spacing();

                if (ImGui.Button(Loc.T("project.open")))
                    _context.OpenProjectWithDialog();

                if (ImGui.Button(Loc.T("project.create")))
                    _context.CreateProjectWithDialog();

                return;
            }

            ImGui.Selectable(Path.GetFileName(Path.TrimEndingDirectorySeparator(workspace.RootPath)),
                false, ImGuiSelectableFlags.None, new Vector2(180f, 0f));

            HandleDropTarget(workspace, string.Empty);

            ImGui.SameLine();

            if (ImGui.SmallButton(Loc.T("project.refresh")))
                workspace.Refresh();

            ImGui.Separator();

            _folderRects.Clear();

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

        private void HandleDropTarget(EditorWorkspace workspace, string targetPrefix)
        {
            if (!ImGui.BeginDragDropTarget())
                return;

            if (!ImGui.AcceptDragDropPayload(AssetDragDrop.PayloadType).IsNull)
            {
                string source = AssetDragDrop.DirectoryPath ?? AssetDragDrop.Current?.Path;
                bool isDirectory = AssetDragDrop.DirectoryPath != null;

                if (source != null)
                    _pendingAction = () => ApplyMove(workspace, source, targetPrefix, isDirectory);
            }

            ImGui.EndDragDropTarget();
        }

        private void ApplyMove(EditorWorkspace workspace, string source, string targetPrefix, bool isDirectory)
        {
            string oldAbsolute = workspace.ToAbsolute(source);

            if (!workspace.TryMove(source, targetPrefix, isDirectory, out var moved, out var error))
            {
                Debug.LogWarning(Loc.T("project.moveFailed",
                    Path.GetFileName(Path.TrimEndingDirectorySeparator(source)), error));
                return;
            }

            if (string.Equals(moved, source, StringComparison.Ordinal))
                return;

            _context.HandlePathRenamed(oldAbsolute, workspace.ToAbsolute(moved), isDirectory);

            RewriteReferences(workspace, source, moved, isDirectory);
        }

        private void RewriteReferences(EditorWorkspace workspace, string oldRelative, string newRelative, bool isDirectory)
        {
            AssetReferenceRewriter.RewriteProject(workspace, oldRelative, newRelative, isDirectory);
            AssetReferenceRewriter.RewriteScene(_context.Scene, oldRelative, newRelative, isDirectory);
        }

        public string ResolveDropFolder(Vector2 screenPosition)
        {
            if (!IsOpen)
                return string.Empty;

            foreach (var (min, max, prefix) in _folderRects)
            {
                if (screenPosition.X >= min.X && screenPosition.X <= max.X &&
                    screenPosition.Y >= min.Y && screenPosition.Y <= max.Y)
                    return prefix;
            }

            return string.Empty;
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

            ImGui.Text(Loc.T("project.deleteConfirm", Path.GetFileName(Path.TrimEndingDirectorySeparator(_deletePath))));

            if (_deleteIsDirectory)
                ImGui.TextDisabled(Loc.T("project.deleteFolderWarning"));

            ImGui.TextDisabled(Loc.T("project.deleteIrreversible"));
            ImGui.Separator();

            if (ImGui.Button(Loc.T("common.delete"), new System.Numerics.Vector2(120f, 0f)))
            {
                ApplyDelete(workspace);
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button(Loc.T("common.cancel"), new System.Numerics.Vector2(120f, 0f)))
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
                Debug.Log($"Deleted: {_deletePath}");
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to delete", ex);
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
            if (!ImGui.BeginMenu(Loc.T("project.createMenu")))
                return;

            foreach (var creator in AssetCreatorRegistry.Creators)
            {
                if (!ImGui.MenuItem(Loc.T(creator.MenuName)))
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
                        Debug.LogException("Failed to create asset", ex);
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

                _folderRects.Add((ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), prefix + rest + "/"));

                if (ImGui.BeginDragDropSource())
                {
                    AssetDragDrop.SetDirectory(dir);

                    unsafe
                    {
                        byte dummy = 0;
                        ImGui.SetDragDropPayload(AssetDragDrop.PayloadType, &dummy, 1);
                    }

                    ImGui.Text(rest);
                    ImGui.EndDragDropSource();
                }

                HandleDropTarget(workspace, prefix + rest + "/");

                if (ImGui.BeginPopupContextItem())
                {
                    DrawCreateMenu(workspace, prefix + rest + "/");

                    ImGui.Separator();

                    if (ImGui.MenuItem(Loc.T("common.rename")))
                        StartRename(dir, isDirectory: true);

                    if (ImGui.MenuItem(Loc.T("common.delete")))
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
                    if (asset.AssetType == typeof(Iris.Assets.ITexture) && ImGui.MenuItem(Loc.T("project.sliceSprite")))
                        _context.RequestOpenSpriteSlicer(asset.Path);

                    if (ImGui.MenuItem(Loc.T("common.rename")))
                        StartRename(asset.Path, isDirectory: false);

                    if (ImGui.MenuItem(Loc.T("common.delete")))
                        RequestDelete(asset.Path, isDirectory: false);

                    ImGui.EndPopup();
                }

                if (ImGui.BeginDragDropSource())
                {
                    AssetDragDrop.SetAsset(asset);

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
                Debug.LogError($"Failed to rename: {error}");
                return;
            }

            string newAbsolute = Path.Combine(Path.GetDirectoryName(oldAbsolute), newName);
            _context.HandlePathRenamed(oldAbsolute, newAbsolute, isDirectory);

            string directory = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? string.Empty;
            string newRelative = directory.Length == 0 ? newName : directory + "/" + newName;

            RewriteReferences(workspace, relativePath, newRelative, isDirectory);
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
                Debug.LogException("Failed to open scene", ex);
            }
        }
    }
}
