using Hexa.NET.ImGui;
using IrisEditor.Panels;
using IrisEditor.Platform;
using IrisEditor.Rendering;
using IrisEditor.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace IrisEditor
{
    internal sealed unsafe class EditorApp
    {
        private readonly EditorContext _context;
        private readonly HierarchyPanel _hierarchy;
        private readonly ScenePanel _scene;
        private readonly CameraPanel _camera;
        private readonly InspectorPanel _inspector;
        private readonly ProjectPanel _project;
        private readonly TilePalettePanel _tiles;

        private readonly List<EditorPanel> _panels;
        private readonly UIEditorPanel _uiEditor;
        private readonly SpriteSlicerPanel _spriteSlicer;
        private readonly ProjectSettingsPanel _projectSettings;
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

            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            var fontPath = @"C:\Windows\Fonts\malgun.ttf";
            if (File.Exists(fontPath))
                io.Fonts.AddFontFromFileTTF(fontPath, 16f);

            _panels = new List<EditorPanel> { _hierarchy, _scene, _camera, _inspector, _project, _tiles };
        }

        public void Draw()
        {
            FileDialog.Update();
            _context.Scripts.Update();
            _context.Builder.Update();

            if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.S) && _context.Dirty)
                SaveScene(saveAs: false);

            if (ImGui.IsKeyPressed(ImGuiKey.F5, false))
                _context.RunGame();

            DrawMainMenuBar();
            DrawDockspace();

            foreach (var panel in _panels)
                panel.Draw();

            _uiEditor.Draw();
            _spriteSlicer.Draw();
            _projectSettings.Draw();
        }

        private void DrawMainMenuBar()
        {
            if (!ImGui.BeginMainMenuBar())
                return;

            if (ImGui.BeginMenu("파일"))
            {
                if (ImGui.MenuItem("새 프로젝트"))
                    _context.CreateProjectWithDialog();

                ImGui.Separator();

                if (ImGui.MenuItem("씬 저장", "Ctrl+S"))
                    SaveScene(saveAs: false);

                if (ImGui.MenuItem("다른 이름으로 저장"))
                    SaveScene(saveAs: true);

                ImGui.Separator();

                if (ImGui.MenuItem("프로젝트 열기"))
                    _context.OpenProjectWithDialog();

                if (ImGui.MenuItem("스크립트 새로고침", string.Empty, false, !_context.Scripts.Building))
                    _context.RefreshScripts();

                ImGui.Separator();

                if (ImGui.MenuItem("현재 씬 실행", "F5", false, !_context.Scripts.Building && !_context.Builder.Building))
                    _context.RunGame();

                ImGui.Separator();

                if (ImGui.MenuItem(_context.Builder.Building ? "빌드 중..." : "빌드", string.Empty, false, !_context.Builder.Building))
                    _context.BuildGameWithDialog();

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("프로젝트"))
            {
                if (ImGui.MenuItem("프로젝트 설정", string.Empty, false, _context.Workspace != null))
                    _projectSettings.Open();

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("보기"))
            {
                foreach (var panel in _panels)
                    ImGui.MenuItem(panel.Title, string.Empty, ref panel.IsOpen);

                ImGui.Separator();

                ImGui.MenuItem("콜라이더 표시", string.Empty, ref _scene.ShowColliders);

                ImGui.Separator();

                if (ImGui.MenuItem("레이아웃 초기화"))
                    _resetLayout = true;

                ImGui.EndMenu();
            }

            string sceneLabel = _context.ScenePath == null ? "제목 없는 씬" : Path.GetFileName(_context.ScenePath);
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
                Console.WriteLine($"[에디터] 씬 저장 실패: {ex.Message}");
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

            if (_resetLayout || ImGuiP.DockBuilderGetNode(dockspaceId).IsNull)
            {
                _resetLayout = false;
                BuildDefaultLayout(dockspaceId, viewport.WorkSize);
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
            ImGuiP.DockBuilderDockWindow(_hierarchy.Title, hierarchy);
            ImGuiP.DockBuilderDockWindow(_scene.Title, scene);
            ImGuiP.DockBuilderDockWindow(_camera.Title, scene);

            ImGuiP.DockBuilderFinish(dockspaceId);

            ImGui.SetWindowFocus(_scene.Title);
        }
    }
}
