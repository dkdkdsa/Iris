using Hexa.NET.ImGui;
using Iris;
using Iris.Debugging;
using IrisEditor.Data;
using IrisEditor.Serialization;
using IrisEditor.Workspace;
using System;
using System.IO;
using System.Numerics;
using System.Text.Json.Nodes;

namespace IrisEditor.Panels
{
    internal sealed class ProjectSettingsPanel
    {
        private static readonly DebugChannel _log = Debug.Channel("Editor");

        private static readonly (string Label, int Width, int Height)[] _resolutions =
        {
            ("800 × 600", 800, 600),
            ("1024 × 768", 1024, 768),
            ("1280 × 720", 1280, 720),
            ("1600 × 900", 1600, 900),
            ("1920 × 1080", 1920, 1080),
        };

        private readonly EditorContext _context;

        private bool _open;
        private string _path;
        private string _rootPath;
        private ProjectConfig _config = new();
        private JsonObject _raw = new();
        private bool _dirty;

        public ProjectSettingsPanel(EditorContext context)
        {
            _context = context;
        }

        public void Open()
        {
            var workspace = _context.Workspace;

            if (workspace == null)
            {
                _log.LogWarning("열린 프로젝트가 없습니다. 프로젝트를 먼저 여세요.");
                return;
            }

            Reload();
            _open = true;
        }

        public void Draw()
        {
            if (!_open)
                return;

            var workspace = _context.Workspace;

            if (workspace != null && !string.Equals(workspace.RootPath, _rootPath, StringComparison.OrdinalIgnoreCase))
                Reload();

            ImGui.SetNextWindowSize(new Vector2(520f, 0f), ImGuiCond.FirstUseEver);

            string title = $"프로젝트 설정{(_dirty ? " *" : "")}###ProjectSettingsWindow";

            if (ImGui.Begin(title, ref _open))
                DrawContent();

            ImGui.End();
        }

        private void Reload()
        {
            var workspace = _context.Workspace;

            if (workspace == null)
            {
                _path = null;
                _rootPath = null;
                _config = new ProjectConfig();
                _raw = new JsonObject();
                _dirty = false;
                return;
            }

            _rootPath = workspace.RootPath;
            _path = Path.Combine(workspace.RootPath, ProjectConfigSerializer.FileName);

            try
            {
                _config = ProjectConfigSerializer.Load(_path, out _raw);
                _dirty = false;
            }
            catch (Exception ex)
            {
                _log.LogException("project.json 읽기 실패", ex);
                _config = new ProjectConfig();
                _raw = new JsonObject();
                _dirty = false;
            }
        }

        private void Save()
        {
            if (_path == null)
                return;

            bool created = !File.Exists(_path);

            try
            {
                ProjectConfigSerializer.Save(_path, _config, _raw);
                _config = ProjectConfigSerializer.Load(_path, out _raw);
                _dirty = false;

                if (created)
                    _context.Workspace?.Refresh();

                _log.Log($"프로젝트 설정 저장: {_path}");
            }
            catch (Exception ex)
            {
                _log.LogException("project.json 저장 실패", ex);
            }
        }

        private void DrawBuildScenes(EditorWorkspace workspace)
        {
            ImGui.Spacing();
            ImGui.SeparatorText("빌드 씬 (0번이 시작 씬)");

            var scenes = _config.BuildScenes;
            int moveUp = -1;
            int moveDown = -1;
            int remove = -1;

            for (int i = 0; i < scenes.Count; i++)
            {
                ImGui.PushID(i);

                ImGui.AlignTextToFramePadding();
                ImGui.Text($"{i}");
                ImGui.SameLine();

                string display = string.IsNullOrEmpty(scenes[i])
                    ? "(비어 있음)"
                    : Path.GetFileNameWithoutExtension(scenes[i]);

                ImGui.SetNextItemWidth(-120f);
                ImGui.InputText("##Scene", ref display, 256, ImGuiInputTextFlags.ReadOnly);

                if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(scenes[i]))
                    ImGui.SetTooltip(scenes[i]);

                ImGui.SameLine();
                ImGui.BeginDisabled(i == 0);
                if (ImGui.ArrowButton("up", ImGuiDir.Up))
                    moveUp = i;
                ImGui.EndDisabled();

                ImGui.SameLine();
                ImGui.BeginDisabled(i == scenes.Count - 1);
                if (ImGui.ArrowButton("down", ImGuiDir.Down))
                    moveDown = i;
                ImGui.EndDisabled();

                ImGui.SameLine();
                if (ImGui.SmallButton("X"))
                    remove = i;

                ImGui.PopID();
            }

            if (moveUp > 0)
            {
                (scenes[moveUp - 1], scenes[moveUp]) = (scenes[moveUp], scenes[moveUp - 1]);
                _dirty = true;
            }

            if (moveDown >= 0 && moveDown < scenes.Count - 1)
            {
                (scenes[moveDown + 1], scenes[moveDown]) = (scenes[moveDown], scenes[moveDown + 1]);
                _dirty = true;
            }

            if (remove >= 0)
            {
                scenes.RemoveAt(remove);
                _dirty = true;
            }

            ImGui.SetNextItemWidth(-120f);
            var picked = AssetPicker.Draw("씬 추가", string.Empty, typeof(SceneData), workspace);

            if (picked is JsonValue value && value.TryGetValue(out string scenePath) && !string.IsNullOrWhiteSpace(scenePath))
            {
                scenes.Add(scenePath.Replace('\\', '/'));
                _dirty = true;
            }
        }

        private void DrawContent()
        {
            var workspace = _context.Workspace;

            if (workspace == null)
            {
                ImGui.TextDisabled("열린 프로젝트가 없습니다");
                return;
            }

            ImGui.TextDisabled(_path);
            ImGui.Separator();

            ImGui.SetNextItemWidth(-160f);
            string title = _config.Title ?? string.Empty;

            if (ImGui.InputText("창 제목", ref title, 128))
            {
                _config.Title = title;
                _dirty = true;
            }

            DrawBuildScenes(workspace);

            ImGui.Spacing();
            ImGui.SeparatorText("창");

            ImGui.SetNextItemWidth(-160f);
            int width = _config.DefaultWidth;

            if (ImGui.InputInt("너비", ref width))
            {
                _config.DefaultWidth = Math.Max(1, width);
                _dirty = true;
            }

            ImGui.SetNextItemWidth(-160f);
            int height = _config.DefaultHeight;

            if (ImGui.InputInt("높이", ref height))
            {
                _config.DefaultHeight = Math.Max(1, height);
                _dirty = true;
            }

            ImGui.SetNextItemWidth(-160f);

            if (ImGui.BeginCombo("프리셋", CurrentResolutionLabel()))
            {
                foreach (var resolution in _resolutions)
                {
                    bool selected = resolution.Width == _config.DefaultWidth &&
                                    resolution.Height == _config.DefaultHeight;

                    if (ImGui.Selectable(resolution.Label, selected))
                    {
                        _config.DefaultWidth = resolution.Width;
                        _config.DefaultHeight = resolution.Height;
                        _dirty = true;
                    }
                }

                ImGui.EndCombo();
            }

            bool fullscreen = _config.Fullscreen;

            if (ImGui.Checkbox("전체 화면", ref fullscreen))
            {
                _config.Fullscreen = fullscreen;
                _dirty = true;
            }

            bool resizable = _config.Resizable;

            if (ImGui.Checkbox("크기 조절 가능", ref resizable))
            {
                _config.Resizable = resizable;
                _dirty = true;
            }

            ImGui.Spacing();
            ImGui.SeparatorText("프레임");

            bool vsync = _config.VSync;

            if (ImGui.Checkbox("수직 동기화", ref vsync))
            {
                _config.VSync = vsync;
                _dirty = true;
            }

            ImGui.SetNextItemWidth(-160f);
            int targetFrameRate = _config.TargetFrameRate;

            if (ImGui.InputInt("최대 프레임", ref targetFrameRate))
            {
                _config.TargetFrameRate = Math.Max(0, targetFrameRate);
                _dirty = true;
            }

            ImGui.TextDisabled(FrameRateHint());

            ImGui.Spacing();
            ImGui.Separator();

            ImGui.BeginDisabled(!_dirty);

            if (ImGui.Button("저장", new Vector2(120f, 0f)))
                Save();

            ImGui.SameLine();

            if (ImGui.Button("되돌리기", new Vector2(120f, 0f)))
                Reload();

            ImGui.EndDisabled();

            if (_dirty)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("저장 안 된 변경사항");
            }
        }

        private string FrameRateHint()
        {
            if (_config.TargetFrameRate > 0)
                return _config.VSync
                    ? $"{_config.TargetFrameRate}fps 와 모니터 주사율 중 낮은 쪽으로 제한됩니다"
                    : $"{_config.TargetFrameRate}fps 로 제한됩니다";

            return _config.VSync
                ? "모니터 주사율로 제한됩니다 (최대 프레임 0 = 제한 없음)"
                : "제한이 없어 CPU/GPU를 최대로 사용합니다";
        }

        private string CurrentResolutionLabel()
        {
            foreach (var resolution in _resolutions)
            {
                if (resolution.Width == _config.DefaultWidth && resolution.Height == _config.DefaultHeight)
                    return resolution.Label;
            }

            return "사용자 지정";
        }
    }
}
