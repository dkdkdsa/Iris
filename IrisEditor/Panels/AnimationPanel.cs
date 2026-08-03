using Hexa.NET.ImGui;
using Iris.Debugging;
using IrisEditor.Data;
using IrisEditor.Localization;
using IrisEditor.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json.Nodes;

namespace IrisEditor.Panels
{
    internal sealed class AnimationPanel
    {
        private const float TrackListWidth = 220f;
        private const float RowHeight = 20f;
        private const float RulerHeight = 24f;
        private const float KeyRadius = 5f;

        private static readonly (string Label, AnimationTrackKind Kind)[] _kinds =
        {
            ("sprite", AnimationTrackKind.Sprite),
            ("float", AnimationTrackKind.Float),
            ("vector2", AnimationTrackKind.Vector2),
            ("color", AnimationTrackKind.Color),
        };

        private readonly EditorContext _context;

        private bool _open;
        private string _path;
        private AnimationClipData _clip;
        private bool _dirty;

        private float _time;
        private float _pixelsPerSecond = 240f;
        private int _selectedTrack = -1;
        private int _selectedKey = -1;
        private int _draggingTrack = -1;
        private int _draggingKey = -1;

        private bool _preview;
        private ActorData _previewActor;

        public bool ConsumedSaveShortcut { get; private set; }

        public AnimationPanel(EditorContext context)
        {
            _context = context;
        }

        public void Draw()
        {
            ConsumedSaveShortcut = false;

            string pending = _context.ConsumePendingAnimation();

            if (pending != null)
                Open(pending);

            if (!_open)
            {
                ClearPreview();
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(900f, 420f), ImGuiCond.FirstUseEver);

            string title = $"{Loc.T("animation.title")}{(_dirty ? " *" : "")}###AnimationWindow";

            if (ImGui.Begin(title, ref _open))
                DrawContent();

            ImGui.End();

            ApplyPreview();
        }

        private void ApplyPreview()
        {
            var actor = _context.Selected;

            if (!_preview || _clip == null || actor == null)
            {
                ClearPreview();
                return;
            }

            if (!ReferenceEquals(actor, _previewActor))
            {
                ClearPreview();
                _previewActor = actor;
            }

            foreach (var track in _clip.Tracks)
            {
                var component = Find(actor, track.Component);

                if (component == null)
                    continue;

                var value = AnimationSampler.Evaluate(track, _time);

                if (value != null)
                    component.SetPreview(track.Property, value);
            }
        }

        private void ClearPreview()
        {
            if (_previewActor == null)
                return;

            foreach (var component in _previewActor.Components)
                component.ClearPreview();

            _previewActor = null;
        }

        private static ComponentData Find(ActorData actor, string componentType)
        {
            foreach (var component in actor.Components)
            {
                if (component.TargetType?.FullName == componentType || component.TypeName == componentType)
                    return component;
            }

            return null;
        }

        public void Open(string path)
        {
            try
            {
                _clip = AnimationClipSerializer.Load(path);
                _path = path;
                _open = true;
                _dirty = false;
                _time = 0f;
                _selectedTrack = _clip.Tracks.Count > 0 ? 0 : -1;
                _selectedKey = -1;
            }
            catch (Exception ex)
            {
                Debug.LogException($"Failed to open animation: {Path.GetFileName(path)}", ex);
            }
        }

        private void Save()
        {
            if (_clip == null || _path == null)
                return;

            try
            {
                AnimationClipSerializer.Save(_path, _clip);
                _dirty = false;

                Debug.Log($"Animation saved: {Path.GetFileName(_path)}");
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to save animation", ex);
            }
        }

        private void DrawContent()
        {
            if (_clip == null)
            {
                ImGui.TextDisabled(Loc.T("animation.empty"));
                return;
            }

            DrawToolbar();
            ImGui.Separator();

            float timelineHeight = MathF.Max(ImGui.GetContentRegionAvail().Y - 120f, RulerHeight + RowHeight * 2f);

            DrawTrackList(timelineHeight);
            ImGui.SameLine();
            DrawTimeline(timelineHeight);

            ImGui.Separator();
            DrawKeyInspector();
        }

        private void DrawToolbar()
        {
            bool preview = _preview;

            if (ImGui.Checkbox(Loc.T("animation.preview"), ref preview))
            {
                _preview = preview;

                if (!preview)
                    ClearPreview();
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("animation.previewHint"));

            ImGui.SameLine();
            ImGui.TextDisabled(Path.GetFileName(_path));
            ImGui.SameLine();

            ImGui.SetNextItemWidth(80f);
            int sampleRate = _clip.SampleRate;

            if (ImGui.InputInt(Loc.T("animation.samples"), ref sampleRate))
            {
                _clip.SampleRate = Math.Clamp(sampleRate, 1, 240);
                _dirty = true;
            }

            ImGui.SameLine();
            bool loop = _clip.Loop;

            if (ImGui.Checkbox(Loc.T("animation.loop"), ref loop))
            {
                _clip.Loop = loop;
                _dirty = true;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(120f);
            ImGui.SliderFloat(Loc.T("animation.zoom"), ref _pixelsPerSecond, 40f, 1200f, "%.0f px/s");

            ImGui.SameLine();
            ImGui.TextDisabled($"t = {_time:F3}s   /   {_clip.Length:F3}s");

            ImGui.SameLine();
            ImGui.Dummy(new Vector2(MathF.Max(ImGui.GetContentRegionAvail().X - 92f, 0f), 0f));
            ImGui.SameLine();

            if (ImGui.Button(Loc.T("common.save"), new Vector2(80f, 0f)))
                Save();

            if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.S))
            {
                Save();
                ConsumedSaveShortcut = true;
            }
        }

        private void DrawTrackList(float height)
        {
            ImGui.BeginChild("##tracks", new Vector2(TrackListWidth, height), ImGuiChildFlags.Borders);

            ImGui.Dummy(new Vector2(0f, RulerHeight - ImGui.GetStyle().ItemSpacing.Y));

            int remove = -1;

            for (int i = 0; i < _clip.Tracks.Count; i++)
            {
                var track = _clip.Tracks[i];

                ImGui.PushID(i);

                if (ImGui.Selectable(track.Label, _selectedTrack == i, ImGuiSelectableFlags.None,
                        new Vector2(0f, RowHeight - 2f)))
                {
                    _selectedTrack = i;
                    _selectedKey = -1;
                }

                if (ImGui.BeginPopupContextItem("##trackmenu"))
                {
                    if (ImGui.MenuItem(Loc.T("animation.removeTrack")))
                        remove = i;

                    ImGui.EndPopup();
                }

                ImGui.PopID();
            }

            if (remove >= 0)
            {
                _clip.Tracks.RemoveAt(remove);
                _selectedTrack = -1;
                _selectedKey = -1;
                _dirty = true;
            }

            ImGui.Spacing();

            if (ImGui.Button(Loc.T("animation.addProperty"), new Vector2(-1f, 0f)))
                ImGui.OpenPopup("AddTrackPopup");

            DrawAddTrackPopup();

            ImGui.EndChild();
        }

        private void DrawAddTrackPopup()
        {
            if (!ImGui.BeginPopup("AddTrackPopup"))
                return;

            foreach (var type in ComponentCatalog.Types)
            {
                if (!ImGui.BeginMenu(type.Name))
                    continue;

                foreach (var property in type.GetProperties())
                {
                    if (property.GetSetMethod() == null || property.GetGetMethod() == null)
                        continue;

                    if (!TryMapKind(property.PropertyType, out var kind))
                        continue;

                    if (ImGui.MenuItem(property.Name))
                    {
                        _clip.Tracks.Add(new AnimationTrackData
                        {
                            Component = type.FullName,
                            Property = property.Name,
                            Kind = kind,
                        });

                        _selectedTrack = _clip.Tracks.Count - 1;
                        _selectedKey = -1;
                        _dirty = true;
                    }
                }

                ImGui.EndMenu();
            }

            ImGui.Separator();

            foreach (var (label, kind) in _kinds)
            {
                if (!ImGui.MenuItem(Loc.T("animation.customTrack", label)))
                    continue;

                _clip.Tracks.Add(new AnimationTrackData { Kind = kind, Property = "Sprite" });
                _selectedTrack = _clip.Tracks.Count - 1;
                _dirty = true;
            }

            ImGui.EndPopup();
        }

        private static bool TryMapKind(Type type, out AnimationTrackKind kind)
        {
            if (type == typeof(float) || type == typeof(int))
            {
                kind = AnimationTrackKind.Float;
                return true;
            }

            if (type == typeof(Silk.NET.Maths.Vector2D<float>))
            {
                kind = AnimationTrackKind.Vector2;
                return true;
            }

            if (type == typeof(Iris.Core.Color))
            {
                kind = AnimationTrackKind.Color;
                return true;
            }

            if (type == typeof(Iris.Core.Sprite) || type == typeof(Iris.Core.Tile))
            {
                kind = AnimationTrackKind.Sprite;
                return true;
            }

            kind = AnimationTrackKind.Sprite;
            return false;
        }

        private void DrawTimeline(float height)
        {
            ImGui.BeginChild("##timeline", new Vector2(0f, height), ImGuiChildFlags.Borders,
                ImGuiWindowFlags.HorizontalScrollbar);

            float span = MathF.Max(_clip.Length, 1f) + 1f;
            float width = span * _pixelsPerSecond;

            var origin = ImGui.GetCursorScreenPos();
            var draw = ImGui.GetWindowDrawList();

            ImGui.InvisibleButton("##surface", new Vector2(width, RulerHeight + _clip.Tracks.Count * RowHeight + RowHeight));

            bool hovered = ImGui.IsItemHovered();
            var mouse = ImGui.GetIO().MousePos;

            DrawRuler(draw, origin, width, height);
            DrawKeys(draw, origin, hovered, mouse);
            DrawPlayhead(draw, origin, height);

            HandleScrub(origin, hovered, mouse, width);

            ImGui.EndChild();
        }

        private void DrawRuler(ImDrawListPtr draw, Vector2 origin, float width, float height)
        {
            uint line = ImGui.GetColorU32(ImGuiCol.Separator);
            uint text = ImGui.GetColorU32(ImGuiCol.TextDisabled);

            float step = 1f / Math.Max(1, _clip.SampleRate);
            float pixelStep = step * _pixelsPerSecond;

            if (pixelStep >= 4f)
            {
                for (float t = 0f; t * _pixelsPerSecond <= width; t += step)
                {
                    float x = origin.X + t * _pixelsPerSecond;
                    draw.AddLine(new Vector2(x, origin.Y + RulerHeight - 4f), new Vector2(x, origin.Y + RulerHeight), line);
                }
            }

            for (float t = 0f; t * _pixelsPerSecond <= width; t += 1f)
            {
                float x = origin.X + t * _pixelsPerSecond;

                draw.AddLine(new Vector2(x, origin.Y), new Vector2(x, origin.Y + height), line);
                draw.AddText(new Vector2(x + 3f, origin.Y + 3f), text, $"{t:0.##}s");
            }
        }

        private void DrawKeys(ImDrawListPtr draw, Vector2 origin, bool hovered, Vector2 mouse)
        {
            uint idle = ImGui.GetColorU32(ImGuiCol.Text);
            uint active = ImGui.GetColorU32(ImGuiCol.ButtonActive);
            uint rowTint = ImGui.GetColorU32(ImGuiCol.FrameBg);

            bool down = ImGui.IsMouseDown(ImGuiMouseButton.Left);
            bool clicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
            bool released = ImGui.IsMouseReleased(ImGuiMouseButton.Left);

            for (int t = 0; t < _clip.Tracks.Count; t++)
            {
                var track = _clip.Tracks[t];
                float rowY = origin.Y + RulerHeight + t * RowHeight + RowHeight * 0.5f;

                if (t == _selectedTrack)
                    draw.AddRectFilled(
                        new Vector2(origin.X, rowY - RowHeight * 0.5f),
                        new Vector2(origin.X + 100000f, rowY + RowHeight * 0.5f), rowTint);

                for (int k = 0; k < track.Keys.Count; k++)
                {
                    float x = origin.X + track.Keys[k].Time * _pixelsPerSecond;
                    var center = new Vector2(x, rowY);

                    bool isSelected = _selectedTrack == t && _selectedKey == k;
                    bool over = hovered && Vector2.Distance(mouse, center) <= KeyRadius + 2f;

                    Diamond(draw, center, isSelected ? active : idle);

                    if (over && clicked)
                    {
                        _selectedTrack = t;
                        _selectedKey = k;
                        _draggingTrack = t;
                        _draggingKey = k;
                    }

                    if (over && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                    {
                        track.Keys.RemoveAt(k);
                        _selectedKey = -1;
                        _dirty = true;
                        break;
                    }
                }
            }

            if (released)
            {
                _draggingTrack = -1;
                _draggingKey = -1;
            }

            if (down && _draggingTrack >= 0 && _draggingTrack < _clip.Tracks.Count)
            {
                var track = _clip.Tracks[_draggingTrack];

                if (_draggingKey >= 0 && _draggingKey < track.Keys.Count)
                {
                    track.Keys[_draggingKey].Time = Snap(MathF.Max(0f, (mouse.X - origin.X) / _pixelsPerSecond));
                    _dirty = true;
                }
            }
        }

        private void DrawPlayhead(ImDrawListPtr draw, Vector2 origin, float height)
        {
            float x = origin.X + _time * _pixelsPerSecond;
            uint color = ImGui.GetColorU32(ImGuiCol.SliderGrabActive);

            draw.AddLine(new Vector2(x, origin.Y), new Vector2(x, origin.Y + height), color, 2f);
            draw.AddTriangleFilled(
                new Vector2(x - 5f, origin.Y),
                new Vector2(x + 5f, origin.Y),
                new Vector2(x, origin.Y + 8f), color);
        }

        private void HandleScrub(Vector2 origin, bool hovered, Vector2 mouse, float width)
        {
            if (!hovered || _draggingKey >= 0)
                return;

            bool onRuler = mouse.Y <= origin.Y + RulerHeight;

            if (onRuler && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                _time = Snap(Math.Clamp((mouse.X - origin.X) / _pixelsPerSecond, 0f, width / _pixelsPerSecond));

            if (!onRuler && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) &&
                _selectedTrack >= 0 && _selectedTrack < _clip.Tracks.Count)
            {
                var track = _clip.Tracks[_selectedTrack];

                track.Keys.Add(new AnimationKeyData
                {
                    Time = Snap(MathF.Max(0f, (mouse.X - origin.X) / _pixelsPerSecond)),
                    Value = AnimationClipSerializer.DefaultValue(track.Kind),
                });

                track.Sort();
                _dirty = true;
            }
        }

        private float Snap(float time)
        {
            float step = 1f / Math.Max(1, _clip.SampleRate);

            return MathF.Round(time / step) * step;
        }

        private void DrawKeyInspector()
        {
            if (_selectedTrack < 0 || _selectedTrack >= _clip.Tracks.Count)
            {
                ImGui.TextDisabled(Loc.T("animation.selectTrack"));
                return;
            }

            var track = _clip.Tracks[_selectedTrack];

            ImGui.SetNextItemWidth(200f);
            string component = track.Component;

            if (ImGui.InputText(Loc.T("animation.component"), ref component, 256))
            {
                track.Component = component;
                _dirty = true;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(160f);
            string property = track.Property;

            if (ImGui.InputText(Loc.T("animation.property"), ref property, 128))
            {
                track.Property = property;
                _dirty = true;
            }

            ImGui.SameLine();
            ImGui.TextDisabled($"({track.Keys.Count} keys)");

            if (_selectedKey < 0 || _selectedKey >= track.Keys.Count)
            {
                ImGui.TextDisabled(Loc.T("animation.selectKey"));
                return;
            }

            var key = track.Keys[_selectedKey];

            ImGui.SetNextItemWidth(120f);
            float time = key.Time;

            if (ImGui.InputFloat(Loc.T("animation.time"), ref time, 0f, 0f, "%.3f"))
            {
                key.Time = MathF.Max(0f, time);
                track.Sort();
                _dirty = true;
            }

            ImGui.SameLine();
            DrawValueEditor(track, key);
        }

        private void DrawValueEditor(AnimationTrackData track, AnimationKeyData key)
        {
            switch (track.Kind)
            {
                case AnimationTrackKind.Float:
                {
                    float value = ValueOf(key.Value, 0f);
                    ImGui.SetNextItemWidth(140f);

                    if (ImGui.InputFloat(Loc.T("animation.value"), ref value))
                    {
                        key.Value = JsonValue.Create(value);
                        _dirty = true;
                    }

                    break;
                }

                case AnimationTrackKind.Vector2:
                {
                    var vector = new Vector2(ElementOf(key.Value, 0, 0f), ElementOf(key.Value, 1, 0f));
                    ImGui.SetNextItemWidth(200f);

                    if (ImGui.InputFloat2(Loc.T("animation.value"), ref vector))
                    {
                        key.Value = new JsonArray(JsonValue.Create(vector.X), JsonValue.Create(vector.Y));
                        _dirty = true;
                    }

                    break;
                }

                case AnimationTrackKind.Color:
                {
                    var color = new Vector4(
                        ElementOf(key.Value, 0, 255f) / 255f, ElementOf(key.Value, 1, 255f) / 255f,
                        ElementOf(key.Value, 2, 255f) / 255f, ElementOf(key.Value, 3, 255f) / 255f);

                    ImGui.SetNextItemWidth(220f);

                    if (ImGui.ColorEdit4(Loc.T("animation.value"), ref color))
                    {
                        key.Value = new JsonArray(
                            JsonValue.Create(MathF.Round(color.X * 255f)), JsonValue.Create(MathF.Round(color.Y * 255f)),
                            JsonValue.Create(MathF.Round(color.Z * 255f)), JsonValue.Create(MathF.Round(color.W * 255f)));

                        _dirty = true;
                    }

                    break;
                }

                default:
                {
                    if (key.Value is JsonObject inline)
                    {
                        ImGui.TextDisabled(
                            $"{inline["texture"]?.GetValue<string>()}  [{ValueOf(inline["x"], 0f):0} {ValueOf(inline["y"], 0f):0} " +
                            $"{ValueOf(inline["w"], 0f):0}x{ValueOf(inline["h"], 0f):0}]");

                        break;
                    }

                    var picked = AssetPicker.Draw(Loc.T("animation.value"),
                        key.Value is JsonValue v && v.TryGetValue(out string s) ? s : string.Empty,
                        typeof(Iris.Core.Sprite), _context.Workspace);

                    if (picked != null)
                    {
                        key.Value = picked.DeepClone();
                        _dirty = true;
                    }

                    break;
                }
            }
        }

        private static void Diamond(ImDrawListPtr draw, Vector2 center, uint color)
        {
            draw.AddQuadFilled(
                new Vector2(center.X, center.Y - KeyRadius),
                new Vector2(center.X + KeyRadius, center.Y),
                new Vector2(center.X, center.Y + KeyRadius),
                new Vector2(center.X - KeyRadius, center.Y), color);
        }

        private static float ValueOf(JsonNode node, float fallback)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? f : fallback;
        }

        private static float ElementOf(JsonNode node, int index, float fallback)
        {
            return node is JsonArray array && index < array.Count ? ValueOf(array[index], fallback) : fallback;
        }
    }
}
