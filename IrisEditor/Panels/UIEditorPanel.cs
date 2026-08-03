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
using IrisEditor.Localization;

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

            string title = $"{Loc.T("uiEditor.title")} - {Path.GetFileName(_path)}{(_dirty ? " *" : "")}###UIEditorWindow";

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
                Debug.LogWarning("Discarding unsaved UI changes and opening another file.");

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
                Debug.LogException("Failed to open UI layout", ex);
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
                Debug.LogException("Failed to save UI layout", ex);
            }
        }

        private void DrawMenuBar()
        {
            if (!ImGui.BeginMenuBar())
                return;

            if (ImGui.BeginMenu(Loc.T("menu.file")))
            {
                if (ImGui.MenuItem(Loc.T("common.save")))
                    Save();

                if (ImGui.MenuItem(Loc.T("common.close")))
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

            ImGui.TextDisabled(Loc.T("uiEditor.previewResolution"));

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
            ImGui.TextDisabled(Loc.T("uiEditor.addObject"));
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
            ImGui.TextDisabled(Loc.T("uiEditor.objectInfo"));
            ImGui.Separator();

            if (_selected == null)
            {
                ImGui.TextDisabled(Loc.T("uiEditor.selectHint"));
                return;
            }

            string title = _selected.TargetType?.Name
                ?? (_selected.TypeName != null ? Loc.T("common.unresolved", _selected.TypeName) : Loc.T("common.unknown"));

            ImGui.Text(title);

            if (ImGui.Button(Loc.T("common.delete"), new System.Numerics.Vector2(-1f, 0f)))
            {
                _entries.Remove(_selected);
                _selected = null;
                MarkChanged();
                return;
            }

            ImGui.Separator();
            DrawParentPicker();
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

        private void DrawParentPicker()
        {
            var current = _entries.Find(e => _selected.ParentId is Guid id && e.Id == id);
            string preview = current != null ? EntryLabel(current) : Loc.T("uiEditor.noParent");

            ImGui.SetNextItemWidth(-1f);

            if (!ImGui.BeginCombo(Loc.T("uiEditor.parent"), preview))
                return;

            if (ImGui.Selectable(Loc.T("uiEditor.noParent"), _selected.ParentId == null))
            {
                _selected.ParentId = null;
                MarkChanged();
            }

            foreach (var entry in _entries)
            {
                if (entry == _selected || IsDescendantOf(entry, _selected))
                    continue;

                if (ImGui.Selectable($"{EntryLabel(entry)}##{entry.Id}", _selected.ParentId == entry.Id))
                {
                    _selected.ParentId = entry.Id;
                    MarkChanged();
                }
            }

            ImGui.EndCombo();
        }

        private bool IsDescendantOf(ComponentData candidate, ComponentData ancestor)
        {
            var current = candidate;

            for (int guard = 0; guard < _entries.Count && current != null; guard++)
            {
                if (current.ParentId is not Guid parentId)
                    return false;

                if (parentId == ancestor.Id)
                    return true;

                current = _entries.Find(e => e.Id == parentId);
            }

            return false;
        }

        private static string EntryLabel(ComponentData entry)
        {
            string type = entry.TargetType?.Name ?? entry.TypeName ?? "?";
            string name = entry.GetString("Name", null);

            return string.IsNullOrWhiteSpace(name) ? type : $"{name} ({type})";
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
                        Debug.LogExceptionOnce($"Failed to build UI preview ({entry.TargetType.Name})", ex);
                        instance = null;
                    }
                }

                _instances.Add(instance);
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_instances[i] == null || _entries[i].ParentId is not Guid parentId)
                    continue;

                int parentIndex = _entries.FindIndex(e => e.Id == parentId);

                if (parentIndex >= 0 && _instances[parentIndex] != null)
                    _instances[i].SetParent(_instances[parentIndex]);
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

                if (instance == null || !instance.IsVisibleInHierarchy)
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

            var frameMin = screenMin;
            var frameSize = screenSize;

            if (instance.Parent != null)
            {
                var parentRect = CalculateRect(instance.Parent, screenMin, screenSize, fit);
                frameMin = new System.Numerics.Vector2(parentRect.Origin.X, parentRect.Origin.Y);
                frameSize = new System.Numerics.Vector2(parentRect.Size.X, parentRect.Size.Y);
            }

            float anchorX = frameMin.X + frameSize.X * instance.Anchor.X;
            float anchorY = frameMin.Y + frameSize.Y * instance.Anchor.Y;

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

                if (instance == null || !instance.IsVisibleInHierarchy)
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
