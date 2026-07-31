using Hexa.NET.ImGui;
using Iris.Core;
using Iris.Debugging;
using Iris.Rendering;
using Iris.UI;
using IrisEditor.Data;
using IrisEditor.Rendering;
using IrisEditor.Serialization;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace IrisEditor.Panels
{
    internal sealed class UIEditorPanel
    {
        private static readonly (string Label, int Width, int Height)[] _resolutions =
        {
            ("800 × 600", 800, 600),
            ("1280 × 720", 1280, 720),
            ("1920 × 1080", 1920, 1080),
        };

        private readonly EditorContext _context;
        private readonly SceneRenderer _renderer;

        private bool _open;
        private string _path;
        private List<ComponentData> _entries = new();
        private ComponentData _selected;
        private bool _dirty;
        private int _resolutionIndex = 1;

        private readonly List<UIObject> _instances = new();
        private readonly List<RenderCommand> _cmdBuffer = new();
        private int _version;
        private int _builtVersion = -1;

        private ComponentData _dragEntry;
        private System.Numerics.Vector2 _dragStartMouse;
        private Vector2D<float> _dragStartPosition;

        public UIEditorPanel(EditorContext context, SceneRenderer renderer)
        {
            _context = context;
            _renderer = renderer;
        }

        public void Draw()
        {
            var pending = _context.ConsumePendingUILayout();

            if (pending != null)
                OpenFile(pending);

            if (!_open)
                return;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(1100f, 640f), ImGuiCond.FirstUseEver);

            string title = $"UI 편집기 - {Path.GetFileName(_path)}{(_dirty ? " *" : "")}###UIEditorWindow";

            if (ImGui.Begin(title, ref _open, ImGuiWindowFlags.MenuBar))
            {
                DrawMenuBar();
                DrawPanes();
            }

            ImGui.End();
        }

        private void OpenFile(string path)
        {
            if (_open && _dirty)
                Debug.LogWarning("저장 안 된 UI 변경사항을 버리고 다른 파일을 엽니다.");

            try
            {
                _entries = UILayoutSerializer.Load(path);
                _path = path;
                _selected = null;
                _dirty = false;
                _version++;
                _open = true;
            }
            catch (Exception ex)
            {
                Debug.LogException("UI 레이아웃 열기 실패", ex);
            }
        }

        private void Save()
        {
            try
            {
                UILayoutSerializer.Save(_entries, _path);
                _dirty = false;
            }
            catch (Exception ex)
            {
                Debug.LogException("UI 레이아웃 저장 실패", ex);
            }
        }

        private void DrawMenuBar()
        {
            if (!ImGui.BeginMenuBar())
                return;

            if (ImGui.BeginMenu("파일"))
            {
                if (ImGui.MenuItem("저장"))
                    Save();

                if (ImGui.MenuItem("닫기"))
                    _open = false;

                ImGui.EndMenu();
            }

            ImGui.SetNextItemWidth(140f);

            if (ImGui.BeginCombo("##UIRes", _resolutions[_resolutionIndex].Label))
            {
                for (int i = 0; i < _resolutions.Length; i++)
                {
                    if (ImGui.Selectable(_resolutions[i].Label, i == _resolutionIndex))
                        _resolutionIndex = i;
                }

                ImGui.EndCombo();
            }

            ImGui.TextDisabled("미리보기 해상도");

            ImGui.EndMenuBar();
        }

        private void DrawPanes()
        {
            const float paletteWidth = 180f;
            const float infoWidth = 280f;

            float designWidth = ImGui.GetContentRegionAvail().X - paletteWidth - infoWidth
                                - ImGui.GetStyle().ItemSpacing.X * 2f;

            ImGui.BeginChild("UIPalette", new System.Numerics.Vector2(paletteWidth, 0f), ImGuiChildFlags.Borders);
            DrawPalette();
            ImGui.EndChild();

            ImGui.SameLine();

            ImGui.BeginChild("UIDesign", new System.Numerics.Vector2(designWidth, 0f), ImGuiChildFlags.Borders);
            DrawDesignSurface();
            ImGui.EndChild();

            ImGui.SameLine();

            ImGui.BeginChild("UIInfo", new System.Numerics.Vector2(infoWidth, 0f), ImGuiChildFlags.Borders);
            DrawInfo();
            ImGui.EndChild();
        }

        private void DrawPalette()
        {
            ImGui.TextDisabled("UI 오브젝트 추가");
            ImGui.Separator();

            foreach (var type in UIObjectCatalog.Types)
            {
                if (!ImGui.Selectable(type.Name))
                    continue;

                var properties = UIObjectCatalog.DefaultProperties(type);
                properties["Anchor"] = new JsonArray(0.5f, 0.5f);

                var entry = new ComponentData
                {
                    Id = Guid.NewGuid(),
                    TargetType = type,
                    TypeName = type.FullName,
                    Properties = properties,
                };

                _entries.Add(entry);
                _selected = entry;
                MarkChanged();
            }
        }

        private void DrawInfo()
        {
            ImGui.TextDisabled("UI 오브젝트 정보");
            ImGui.Separator();

            if (_selected == null)
            {
                ImGui.TextDisabled("(디자인 창에서 선택)");
                return;
            }

            string title = _selected.TargetType?.Name
                ?? (_selected.TypeName != null ? $"{_selected.TypeName} (미해석)" : "(알 수 없음)");

            ImGui.Text(title);

            if (ImGui.Button("삭제", new System.Numerics.Vector2(-1f, 0f)))
            {
                _entries.Remove(_selected);
                _selected = null;
                MarkChanged();
                return;
            }

            ImGui.Separator();

            if (_selected.Properties is JsonObject obj)
            {
                var assetProps = _selected.TargetType != null
                    ? UIObjectCatalog.GetAssetProperties(_selected.TargetType)
                    : null;

                if (PropertyDrawer.Draw(obj, assetProps, _context.Workspace, HiddenMembers.Get(_selected.TargetType)))
                    MarkChanged();
            }
        }

        private void MarkChanged()
        {
            _dirty = true;
            _version++;
        }

        private void RebuildInstances()
        {
            if (_builtVersion == _version)
                return;

            foreach (var instance in _instances)
                instance?.Dispose();

            _instances.Clear();

            foreach (var entry in _entries)
            {
                UIObject instance = null;

                if (entry.TargetType != null)
                {
                    try
                    {
                        instance = (UIObject)Activator.CreateInstance(entry.TargetType);
                        JsonPropertyMapper.ApplyProperties(instance, entry.Properties as JsonObject);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogExceptionOnce($"UI 미리보기 생성 실패({entry.TargetType.Name})", ex);
                        instance = null;
                    }
                }

                _instances.Add(instance);
            }

            _builtVersion = _version;
        }

        private void DrawDesignSurface()
        {
            var draw = ImGui.GetWindowDrawList();
            var origin = ImGui.GetCursorScreenPos();
            var avail = ImGui.GetContentRegionAvail();

            if (avail.X < 1f || avail.Y < 1f)
                return;

            ImGui.InvisibleButton("##UIDesignCanvas", avail);
            bool canvasHovered = ImGui.IsItemHovered();

            draw.AddRectFilled(origin, origin + avail, 0xFF181818);

            float refWidth = _resolutions[_resolutionIndex].Width;
            float refHeight = _resolutions[_resolutionIndex].Height;

            float fit = MathF.Min(avail.X / refWidth, avail.Y / refHeight) * 0.95f;
            var screenSize = new System.Numerics.Vector2(refWidth, refHeight) * fit;
            var screenMin = origin + (avail - screenSize) * 0.5f;
            var screenMax = screenMin + screenSize;

            draw.AddRectFilled(screenMin, screenMax, 0xFF000000);
            draw.AddRect(screenMin - System.Numerics.Vector2.One, screenMax + System.Numerics.Vector2.One, 0xFF505050);

            RebuildInstances();

            for (int i = 0; i < _entries.Count; i++)
            {
                var instance = _instances[i];

                if (instance == null || !instance.Visible)
                    continue;

                var rect = CalculateRect(instance, screenMin, screenSize, fit);

                DrawInstance(draw, instance, rect);

                if (_entries[i] == _selected)
                    draw.AddRect(
                        new System.Numerics.Vector2(rect.Origin.X, rect.Origin.Y) - System.Numerics.Vector2.One,
                        new System.Numerics.Vector2(rect.Origin.X + rect.Size.X, rect.Origin.Y + rect.Size.Y) + System.Numerics.Vector2.One,
                        0xFF00A0FF);
            }

            HandleDesignInput(screenMin, screenSize, fit, canvasHovered);
        }

        private Rectangle<float> CalculateRect(UIObject instance, System.Numerics.Vector2 screenMin, System.Numerics.Vector2 screenSize, float fit)
        {
            var size = instance.GetSize() * fit;

            float anchorX = screenMin.X + screenSize.X * instance.Anchor.X;
            float anchorY = screenMin.Y + screenSize.Y * instance.Anchor.Y;

            float width = MathF.Abs(size.X);
            float height = MathF.Abs(size.Y);
            float pivotX = size.X < 0f ? 1f - instance.Anchor.X : instance.Anchor.X;
            float pivotY = size.Y < 0f ? 1f - instance.Anchor.Y : instance.Anchor.Y;

            float x = anchorX + instance.Position.X * fit - width * pivotX;
            float y = anchorY + instance.Position.Y * fit - height * pivotY;

            return new Rectangle<float>(x, y, width, height);
        }

        private unsafe void DrawInstance(ImDrawListPtr draw, UIObject instance, Rectangle<float> rect)
        {
            _cmdBuffer.Clear();
            instance.Render(rect, _cmdBuffer);

            bool drewAnything = false;

            foreach (var cmd in _cmdBuffer)
            {
                if (cmd.texture == null)
                    continue;

                var texRef = new ImTextureRef(null, cmd.texture.Handle);

                var min = new System.Numerics.Vector2(cmd.dest.Origin.X, cmd.dest.Origin.Y);
                var max = new System.Numerics.Vector2(cmd.dest.Origin.X + cmd.dest.Size.X, cmd.dest.Origin.Y + cmd.dest.Size.Y);

                System.Numerics.Vector2 uv0 = System.Numerics.Vector2.Zero;
                System.Numerics.Vector2 uv1 = System.Numerics.Vector2.One;

                if (cmd.src.HasValue)
                {
                    var src = cmd.src.Value;
                    uv0 = new System.Numerics.Vector2(src.Origin.X / (float)cmd.texture.Width, src.Origin.Y / (float)cmd.texture.Height);
                    uv1 = new System.Numerics.Vector2((src.Origin.X + src.Size.X) / (float)cmd.texture.Width, (src.Origin.Y + src.Size.Y) / (float)cmd.texture.Height);
                }

                if (cmd.flipX)
                    (uv0.X, uv1.X) = (uv1.X, uv0.X);

                if (cmd.flipY)
                    (uv0.Y, uv1.Y) = (uv1.Y, uv0.Y);

                uint color = 0xFFFFFFFF;

                if (cmd.color.HasValue)
                {
                    var c = cmd.color.Value;
                    color = (uint)(c.a << 24 | c.b << 16 | c.g << 8 | c.r);
                }

                draw.AddImage(texRef, min, max, uv0, uv1, color);
                drewAnything = true;
            }

            if (instance is UIText text)
                text.Font?.Commit();

            if (!drewAnything)
            {
                var min = new System.Numerics.Vector2(rect.Origin.X, rect.Origin.Y);
                var max = new System.Numerics.Vector2(rect.Origin.X + MathF.Max(rect.Size.X, 24f), rect.Origin.Y + MathF.Max(rect.Size.Y, 24f));
                draw.AddRect(min, max, 0x80FFFFFF);
            }
        }

        private void HandleDesignInput(System.Numerics.Vector2 screenMin, System.Numerics.Vector2 screenSize, float fit, bool canvasHovered)
        {
            if (canvasHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                var mouse = ImGui.GetMousePos();
                _selected = Pick(mouse, screenMin, screenSize, fit);

                if (_selected != null)
                {
                    int index = _entries.IndexOf(_selected);
                    _dragEntry = _selected;
                    _dragStartMouse = mouse;
                    _dragStartPosition = _instances[index]?.Position ?? default;
                }
            }

            if (_dragEntry == null)
                return;

            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                _dragEntry = null;
                return;
            }

            var delta = ImGui.GetMousePos() - _dragStartMouse;

            if (delta.X == 0f && delta.Y == 0f)
                return;

            _dragEntry.SetVector2("Position", new System.Numerics.Vector2(
                _dragStartPosition.X + delta.X / fit,
                _dragStartPosition.Y + delta.Y / fit));

            MarkChanged();
        }

        private ComponentData Pick(System.Numerics.Vector2 mouse, System.Numerics.Vector2 screenMin, System.Numerics.Vector2 screenSize, float fit)
        {
            ComponentData best = null;
            int bestOrder = int.MinValue;

            for (int i = 0; i < _entries.Count; i++)
            {
                var instance = _instances.Count > i ? _instances[i] : null;

                if (instance == null || !instance.Visible)
                    continue;

                var rect = CalculateRect(instance, screenMin, screenSize, fit);

                float width = MathF.Max(rect.Size.X, 24f);
                float height = MathF.Max(rect.Size.Y, 24f);

                if (mouse.X < rect.Origin.X || mouse.X > rect.Origin.X + width ||
                    mouse.Y < rect.Origin.Y || mouse.Y > rect.Origin.Y + height)
                    continue;

                if (instance.Order >= bestOrder)
                {
                    best = _entries[i];
                    bestOrder = instance.Order;
                }
            }

            return best;
        }
    }
}
