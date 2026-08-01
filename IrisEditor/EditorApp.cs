using Hexa.NET.ImGui;
using Iris.Debugging;
using IrisEditor.Panels;
using IrisEditor.Platform;
using IrisEditor.Rendering;
using IrisEditor.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using IrisEditor.Localization;

namespace IrisEditor
{
    internal sealed unsafe class EditorApp
    {
        private const int LayoutVersion = 1;

        private readonly EditorContext _context;
        private readonly HierarchyPanel _hierarchy;
        private readonly ScenePanel _scene;
        private readonly CameraPanel _camera;
        private readonly InspectorPanel _inspector;
        private readonly ProjectPanel _project;
        private readonly TilePalettePanel _tiles;
        private readonly ConsolePanel _console;

        private readonly List<EditorPanel> _panels;
        private readonly UIEditorPanel _uiEditor;
        private readonly SpriteSlicerPanel _spriteSlicer;
        private readonly ProjectSettingsPanel _projectSettings;
        private readonly AnimatorPanel _animator;
        private bool _resetLayout;

        public EditorApp(EditorContext context)
        {
            _context = context;
            var renderer = new SceneRenderer(context);

            _hierarchy = new HierarchyPanel(context);
            _tiles = new TilePalettePanel(context);
            _inspector = new InspectorPanel(context, _tiles);
            _scene = new ScenePanel(context, renderer);
            _camera = new CameraPanel(renderer);
            _project = new ProjectPanel(context);
            _uiEditor = new UIEditorPanel(context, renderer);
            _spriteSlicer = new SpriteSlicerPanel(context);
            _projectSettings = new ProjectSettingsPanel(context);
            _animator = new AnimatorPanel(context);
            _console = new ConsolePanel(context);

            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            var fontPath = @"C:\Windows\Fonts\malgun.ttf";
            if (File.Exists(fontPath))
                io.Fonts.AddFontFromFileTTF(fontPath, 16f);

            _panels = new List<EditorPanel> { _hierarchy, _scene, _camera, _inspector, _project, _tiles, _console };
        }

        public void Draw()
        {
            Loc.PollHotReload();
            FileDialog.Update();
            _context.Scripts.Update();
            _context.Builder.Update();
            ConsumeDroppedPaths();

            if (ImGui.IsKeyPressed(ImGuiKey.F5, false))
                _context.RunGame();

            DrawMainMenuBar();
            DrawDockspace();

            foreach (var panel in _panels)
                panel.Draw();

            _uiEditor.Draw();
            _spriteSlicer.Draw();
            _projectSettings.Draw();
            _animator.Draw();

            if (!_animator.ConsumedSaveShortcut &&
                ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.S) && _context.Dirty)
                SaveScene(saveAs: false);
        }

        private void ConsumeDroppedPaths()
        {
            if (_context.DroppedPaths.Count == 0)
                return;

            var workspace = _context.Workspace;

            if (workspace == null)
            {
                _context.ClearDroppedPaths();
                Debug.LogWarning(Loc.T("project.dropNoProject"));
                return;
            }

            int imported = 0;
            var viewportPos = ImGui.GetMainViewport().Pos;

            foreach (var drop in _context.DroppedPaths)
            {
                var screen = new Vector2(drop.Position.X, drop.Position.Y) + viewportPos;
                string folder = _project.ResolveDropFolder(screen);

                if (AssetImporter.TryImport(workspace, drop.Path, folder, out _, out var error))
                {
                    imported++;
                }
                else
                {
                    Debug.LogWarning(Loc.T("project.dropFailed",
                        Path.GetFileName(Path.TrimEndingDirectorySeparator(drop.Path)), error));
                }
            }

            _context.ClearDroppedPaths();

            if (imported > 0)
                workspace.Refresh();
        }

        private void DrawMainMenuBar()
        {
            if (!ImGui.BeginMainMenuBar())
                return;

            if (ImGui.BeginMenu(Loc.T("menu.file")))
            {
                if (ImGui.MenuItem(Loc.T("menu.file.newProject")))
                    _context.CreateProjectWithDialog();

                if (ImGui.MenuItem(Loc.T("menu.file.openProject")))
                    _context.OpenProjectWithDialog();

                ImGui.Separator();

                if (ImGui.MenuItem(Loc.T("menu.file.saveScene"), "Ctrl+S"))
                    SaveScene(saveAs: false);

                if (ImGui.MenuItem(Loc.T("menu.file.saveSceneAs")))
                    SaveScene(saveAs: true);

                ImGui.Separator();

                if (ImGui.MenuItem(Loc.T("menu.file.refreshScripts"), string.Empty, false, !_context.Scripts.Building))
                    _context.RefreshScripts();

                ImGui.Separator();

                if (ImGui.MenuItem(Loc.T("menu.file.runScene"), "F5", false, !_context.Scripts.Building && !_context.Builder.Building))
                    _context.RunGame();

                ImGui.Separator();

                if (ImGui.MenuItem(_context.Builder.Building ? Loc.T("menu.file.building") : Loc.T("menu.file.build"), string.Empty, false, !_context.Builder.Building))
                    _context.BuildGameWithDialog();

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu(Loc.T("menu.project")))
            {
                if (ImGui.MenuItem(Loc.T("menu.project.settings"), string.Empty, false, _context.Workspace != null))
                    _projectSettings.Open();

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu(Loc.T("menu.view")))
            {
                foreach (var panel in _panels)
                    ImGui.MenuItem(panel.Title, string.Empty, ref panel.IsOpen);

                ImGui.Separator();

                ImGui.MenuItem(Loc.T("menu.view.showColliders"), string.Empty, ref _scene.ShowColliders);

                ImGui.Separator();

                if (ImGui.MenuItem(Loc.T("menu.view.resetLayout")))
                    _resetLayout = true;

                ImGui.Separator();

                if (ImGui.BeginMenu(Loc.T("menu.view.language")))
                {
                    foreach (var code in Loc.Available)
                    {
                        if (ImGui.MenuItem(Loc.DisplayName(code), string.Empty, code == Loc.Language))
                        {
                            Loc.SetLanguage(code);
                            EditorSettings.Language = code;
                            EditorSettings.Save();
                        }
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem(Loc.T("menu.view.reloadLanguage")))
                        Loc.Reload();

                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }

            string sceneLabel = _context.ScenePath == null ? Loc.T("menu.untitledScene") : Path.GetFileName(_context.ScenePath);
            if (_context.Dirty)
                sceneLabel += " *";

            ImGui.TextDisabled(sceneLabel);

            ImGui.EndMainMenuBar();
        }

        private void SaveScene(bool saveAs)
        {
            if (!saveAs && _context.ScenePath != null)
            {
                TrySave(_context.ScenePath);
                return;
            }

            FileDialog.Save(path =>
            {
                if (path != null)
                    TrySave(path);
            });
        }

        private void TrySave(string path)
        {
            try
            {
                _context.SaveScene(path);
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to save scene", ex);
            }
        }

        private void DrawDockspace()
        {
            var viewport = ImGui.GetMainViewport();

            ImGui.SetNextWindowPos(viewport.WorkPos);
            ImGui.SetNextWindowSize(viewport.WorkSize);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse
                      | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                      | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus
                      | ImGuiWindowFlags.NoDocking;

            ImGui.Begin("##EditorDockHost", flags);
            ImGui.PopStyleVar(3);

            uint dockspaceId = ImGui.GetID("EditorDockspace");

            bool outdated = EditorSettings.LayoutVersion != LayoutVersion;

            if (_resetLayout || outdated || ImGuiP.DockBuilderGetNode(dockspaceId).IsNull)
            {
                _resetLayout = false;
                BuildDefaultLayout(dockspaceId, viewport.WorkSize);

                if (outdated)
                {
                    EditorSettings.LayoutVersion = LayoutVersion;
                    EditorSettings.Save();
                }
            }

            ImGui.DockSpace(dockspaceId, Vector2.Zero, ImGuiDockNodeFlags.None);
            ImGui.End();
        }

        private void BuildDefaultLayout(uint dockspaceId, Vector2 size)
        {
            ImGuiP.DockBuilderRemoveNode(dockspaceId);
            ImGuiP.DockBuilderAddNode(dockspaceId, (ImGuiDockNodeFlags)(1 << 10));
            ImGuiP.DockBuilderSetNodeSize(dockspaceId, size);

            uint main = dockspaceId;
            uint inspector, project, hierarchy, scene;

            ImGuiP.DockBuilderSplitNode(main, ImGuiDir.Right, 0.30f, &inspector, &main);
            ImGuiP.DockBuilderSplitNode(main, ImGuiDir.Down, 0.38f, &project, &main);
            ImGuiP.DockBuilderSplitNode(main, ImGuiDir.Left, 0.28f, &hierarchy, &scene);

            ImGuiP.DockBuilderDockWindow(_inspector.Title, inspector);
            ImGuiP.DockBuilderDockWindow(_project.Title, project);
            ImGuiP.DockBuilderDockWindow(_tiles.Title, project);
            ImGuiP.DockBuilderDockWindow(_console.Title, project);
            ImGuiP.DockBuilderDockWindow(_hierarchy.Title, hierarchy);
            ImGuiP.DockBuilderDockWindow(_scene.Title, scene);
            ImGuiP.DockBuilderDockWindow(_camera.Title, scene);

            ImGuiP.DockBuilderFinish(dockspaceId);

            ImGui.SetWindowFocus(_scene.Title);
        }
    }
}
