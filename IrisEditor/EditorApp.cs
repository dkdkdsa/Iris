using Hexa.NET.ImGui;
using IrisEditor.Panels;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace IrisEditor
{
    /// <summary>메뉴바·도킹 레이아웃·패널 목록을 소유하는 에디터 셸.</summary>
    public sealed unsafe class EditorApp
    {
        private readonly HierarchyPanel _hierarchy = new();
        private readonly ScenePanel _scene = new();
        private readonly InspectorPanel _inspector = new();
        private readonly ProjectPanel _project = new();

        private readonly List<EditorPanel> _panels;
        private bool _resetLayout;

        public EditorApp()
        {
            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            // 기본 폰트에는 한글 글리프가 없다. 1.92 동적 폰트라 범위 지정 없이 로드만 하면 된다.
            var fontPath = @"C:\Windows\Fonts\malgun.ttf";
            if (File.Exists(fontPath))
                io.Fonts.AddFontFromFileTTF(fontPath, 16f);

            _panels = new List<EditorPanel> { _hierarchy, _scene, _inspector, _project };
        }

        public void Draw()
        {
            DrawMainMenuBar();
            DrawDockspace();

            foreach (var panel in _panels)
                panel.Draw();
        }

        private void DrawMainMenuBar()
        {
            if (!ImGui.BeginMainMenuBar())
                return;

            if (ImGui.BeginMenu("파일"))
            {
                ImGui.MenuItem("새 씬");
                ImGui.MenuItem("씬 열기");
                ImGui.MenuItem("씬 저장");
                ImGui.Separator();
                ImGui.MenuItem("빌드");
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("보기"))
            {
                foreach (var panel in _panels)
                    ImGui.MenuItem(panel.Title, string.Empty, ref panel.IsOpen);

                ImGui.Separator();

                if (ImGui.MenuItem("레이아웃 초기화"))
                    _resetLayout = true;

                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
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

            // 저장된 레이아웃(imgui.ini)이 없을 때만 기본 배치를 만든다.
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
            // 1 << 10 = ImGuiDockNodeFlags_DockSpace. imgui_internal의 private 플래그라 바인딩 enum에 없다.
            ImGuiP.DockBuilderRemoveNode(dockspaceId);
            ImGuiP.DockBuilderAddNode(dockspaceId, (ImGuiDockNodeFlags)(1 << 10));
            ImGuiP.DockBuilderSetNodeSize(dockspaceId, size);

            // 오른쪽 인스펙터(전체 높이) → 남은 영역 아래 프로젝트 → 남은 영역을 하이어라키/씬으로.
            uint main = dockspaceId;
            uint inspector, project, hierarchy, scene;

            ImGuiP.DockBuilderSplitNode(main, ImGuiDir.Right, 0.30f, &inspector, &main);
            ImGuiP.DockBuilderSplitNode(main, ImGuiDir.Down, 0.38f, &project, &main);
            ImGuiP.DockBuilderSplitNode(main, ImGuiDir.Left, 0.28f, &hierarchy, &scene);

            ImGuiP.DockBuilderDockWindow(_inspector.Title, inspector);
            ImGuiP.DockBuilderDockWindow(_project.Title, project);
            ImGuiP.DockBuilderDockWindow(_hierarchy.Title, hierarchy);
            ImGuiP.DockBuilderDockWindow(_scene.Title, scene);

            ImGuiP.DockBuilderFinish(dockspaceId);
        }
    }
}
