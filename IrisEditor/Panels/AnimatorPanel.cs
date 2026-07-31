using Hexa.NET.ImGui;
using Iris.Core;
using Iris.Debugging;
using IrisEditor.Data;
using IrisEditor.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json.Nodes;
using IrisEditor.Localization;

namespace IrisEditor.Panels
{
    internal sealed class AnimatorPanel
    {
        private static readonly Vector2 NodeSize = new(150f, 44f);
        private const uint NodeColor = 0xFF3A3A3A;
        private const uint NodeDefaultColor = 0xFF2A5A2A;
        private const uint NodeSelectedColor = 0xFF00A0FF;
        private const uint AnyColor = 0xFF2A2A5A;
        private const uint LineColor = 0xFFB0B0B0;
        private const uint LineSelectedColor = 0xFF00A0FF;
        private const float LaneOffset = 7f;
        private const float HitRadius = 6f;

        private readonly EditorContext _context;

        private bool _open;
        private string _path;
        private AnimatorGraph _graph = new();
        private bool _dirty;

        private AnimatorStateData _selectedState;
        private AnimatorTransitionData _selectedTransition;
        private AnimatorStateData _transitionOwner;

        private AnimatorStateData _linkSource;
        private bool _linkFromAny;

        private AnimatorStateData _dragNode;
        private Vector2 _dragOffset;
        private Vector2 _viewOffset;
        private Vector2 _anyPosition = new(40f, 40f);

        private AnimatorStateData _contextNode;
        private bool _contextIsAny;

        public AnimatorPanel(EditorContext context)
        {
            _context = context;
        }

        public bool ConsumedSaveShortcut { get; private set; }

        public void Draw()
        {
            ConsumedSaveShortcut = false;

            var pending = _context.ConsumePendingAnimator();

            if (pending != null)
                Open(pending);

            if (!_open)
                return;

            ImGui.SetNextWindowSize(new Vector2(1080f, 640f), ImGuiCond.FirstUseEver);

            string title = $"{Loc.T("animator.title")} - {Path.GetFileName(_path)}{(_dirty ? " *" : "")}###AnimatorWindow";

            if (ImGui.Begin(title, ref _open, ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) &&
                    ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.S, false))
                {
                    ConsumedSaveShortcut = true;

                    if (_dirty)
                        Save();
                }

                DrawMenuBar();
                DrawPanes();
            }

            ImGui.End();
        }

        private void Open(string path)
        {
            try
            {
                _graph = AnimatorControllerSerializer.Load(path);
                _path = path;
                _selectedState = null;
                _selectedTransition = null;
                _transitionOwner = null;
                _linkSource = null;
                _linkFromAny = false;
                _viewOffset = Vector2.Zero;
                _dirty = false;
                _open = true;
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to open animator controller", ex);
            }
        }

        private void Save()
        {
            try
            {
                AnimatorControllerSerializer.Save(_graph, _path);
                _dirty = false;
                Debug.Log($"Animator saved: {_path}");
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to save animator", ex);
            }
        }

        private void DrawMenuBar()
        {
            if (!ImGui.BeginMenuBar())
                return;

            if (ImGui.BeginMenu(Loc.T("menu.file")))
            {
                if (ImGui.MenuItem(Loc.T("common.save"), "Ctrl+S"))
                    Save();

                if (ImGui.MenuItem(Loc.T("common.close")))
                    _open = false;

                ImGui.EndMenu();
            }

            if (_linkSource != null || _linkFromAny)
                ImGui.TextDisabled(Loc.T("animator.pickTarget"));
            else
                ImGui.TextDisabled(Loc.T("animator.hint"));

            ImGui.EndMenuBar();
        }

        private void DrawPanes()
        {
            const float sideWidth = 230f;
            const float inspectorWidth = 320f;

            float canvasWidth = ImGui.GetContentRegionAvail().X - sideWidth - inspectorWidth
                                - ImGui.GetStyle().ItemSpacing.X * 2f;

            ImGui.BeginChild("AnimatorParams", new Vector2(sideWidth, 0f), ImGuiChildFlags.Borders);
            DrawParameters();
            ImGui.EndChild();

            ImGui.SameLine();

            ImGui.BeginChild("AnimatorCanvas", new Vector2(canvasWidth, 0f), ImGuiChildFlags.Borders);
            DrawCanvas();
            ImGui.EndChild();

            ImGui.SameLine();

            ImGui.BeginChild("AnimatorInspector", new Vector2(inspectorWidth, 0f), ImGuiChildFlags.Borders);
            DrawInspector();
            ImGui.EndChild();
        }

        private void DrawParameters()
        {
            ImGui.TextDisabled(Loc.T("animator.parameters"));
            ImGui.Separator();

            if (ImGui.Button(Loc.T("common.add"), new Vector2(-1f, 0f)))
            {
                _graph.Parameters.Add(new AnimatorParameterData { Name = _graph.UniqueParameterName("New") });
                MarkChanged();
            }

            ImGui.Spacing();

            AnimatorParameterData toRemove = null;

            for (int i = 0; i < _graph.Parameters.Count; i++)
            {
                var parameter = _graph.Parameters[i];
                ImGui.PushID(i);

                ImGui.SetNextItemWidth(-1f);
                string name = parameter.Name ?? string.Empty;

                if (ImGui.InputText("##Name", ref name, 64))
                {
                    parameter.Name = name;
                    MarkChanged();
                }

                ImGui.SetNextItemWidth(-46f);

                if (ImGui.BeginCombo("##Type", parameter.Type.ToString()))
                {
                    foreach (AnimatorParameterType type in Enum.GetValues<AnimatorParameterType>())
                    {
                        if (ImGui.Selectable(type.ToString(), type == parameter.Type))
                        {
                            parameter.Type = type;
                            MarkChanged();
                        }
                    }

                    ImGui.EndCombo();
                }

                ImGui.SameLine();

                if (ImGui.Button("X", new Vector2(-1f, 0f)))
                    toRemove = parameter;

                ImGui.PopID();
                ImGui.Spacing();
            }

            if (toRemove != null)
            {
                _graph.Parameters.Remove(toRemove);
                MarkChanged();
            }
        }

        private void DrawCanvas()
        {
            var draw = ImGui.GetWindowDrawList();
            var origin = ImGui.GetCursorScreenPos();
            var size = ImGui.GetContentRegionAvail();

            if (size.X < 1f || size.Y < 1f)
                return;

            draw.AddRectFilled(origin, origin + size, 0xFF1E1E1E);
            draw.PushClipRect(origin, origin + size, true);

            DrawGrid(draw, origin, size);

            var anyRect = origin + _anyPosition + _viewOffset;
            DrawTransitionLines(draw, origin, anyRect);
            DrawAnyNode(draw, anyRect);

            foreach (var state in _graph.States)
                DrawStateNode(draw, origin + state.Position + _viewOffset, state);

            draw.PopClipRect();

            HandleCanvasInput(origin, size, anyRect);
        }

        private static void DrawGrid(ImDrawListPtr draw, Vector2 origin, Vector2 size)
        {
            const float step = 32f;
            const uint color = 0x20FFFFFF;

            for (float x = 0f; x < size.X; x += step)
                draw.AddLine(new Vector2(origin.X + x, origin.Y), new Vector2(origin.X + x, origin.Y + size.Y), color);

            for (float y = 0f; y < size.Y; y += step)
                draw.AddLine(new Vector2(origin.X, origin.Y + y), new Vector2(origin.X + size.X, origin.Y + y), color);
        }

        private void DrawAnyNode(ImDrawListPtr draw, Vector2 position)
        {
            draw.AddRectFilled(position, position + NodeSize, AnyColor, 4f);
            draw.AddRect(position, position + NodeSize, 0xFF6060A0, 4f);
            draw.AddText(position + new Vector2(10f, 12f), 0xFFFFFFFF, "Any State");
        }

        private void DrawStateNode(ImDrawListPtr draw, Vector2 position, AnimatorStateData state)
        {
            bool isDefault = state.Name == _graph.DefaultState;
            uint fill = isDefault ? NodeDefaultColor : NodeColor;

            draw.AddRectFilled(position, position + NodeSize, fill, 4f);
            draw.AddRect(position, position + NodeSize, state == _selectedState ? NodeSelectedColor : 0xFF808080, 4f,
                ImDrawFlags.None, state == _selectedState ? 2f : 1f);

            draw.AddText(position + new Vector2(10f, 6f), 0xFFFFFFFF, state.Name);

            string clip = string.IsNullOrEmpty(state.Clip) ? Loc.T("animator.noClip") : Path.GetFileNameWithoutExtension(state.Clip);
            draw.AddText(position + new Vector2(10f, 24f), 0xFFA0A0A0, clip);
        }

        private void DrawTransitionLines(ImDrawListPtr draw, Vector2 origin, Vector2 anyRect)
        {
            foreach (var state in _graph.States)
            {
                var from = origin + state.Position + _viewOffset + NodeSize * 0.5f;

                foreach (var transition in state.Transitions)
                {
                    var target = _graph.Find(transition.To);

                    if (target == null)
                        continue;

                    var to = origin + target.Position + _viewOffset + NodeSize * 0.5f;
                    DrawArrow(draw, from, to, transition == _selectedTransition);
                }
            }

            var anyCenter = anyRect + NodeSize * 0.5f;

            foreach (var transition in _graph.AnyTransitions)
            {
                var target = _graph.Find(transition.To);

                if (target == null)
                    continue;

                var to = origin + target.Position + _viewOffset + NodeSize * 0.5f;
                DrawArrow(draw, anyCenter, to, transition == _selectedTransition);
            }
        }

        private static (Vector2 From, Vector2 To) Lane(Vector2 from, Vector2 to)
        {
            var direction = to - from;
            float length = direction.Length();

            if (length < 0.001f)
                return (from, to);

            direction /= length;

            var side = new Vector2(-direction.Y, direction.X) * LaneOffset;
            return (from + side, to + side);
        }

        private static void DrawArrow(ImDrawListPtr draw, Vector2 rawFrom, Vector2 rawTo, bool selected)
        {
            var (from, to) = Lane(rawFrom, rawTo);

            uint color = selected ? LineSelectedColor : LineColor;
            draw.AddLine(from, to, color, selected ? 3f : 1.5f);

            var direction = to - from;
            float length = direction.Length();

            if (length < 1f)
                return;

            direction /= length;

            var mid = (from + to) * 0.5f;
            var side = new Vector2(-direction.Y, direction.X);

            draw.AddTriangleFilled(
                mid + direction * 8f,
                mid - direction * 4f + side * 5f,
                mid - direction * 4f - side * 5f,
                color);
        }

        private void HandleCanvasInput(Vector2 origin, Vector2 size, Vector2 anyRect)
        {
            ImGui.SetCursorScreenPos(origin);
            ImGui.InvisibleButton("##AnimatorCanvasInput", size,
                ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);

            bool hovered = ImGui.IsItemHovered();
            var mouse = ImGui.GetMousePos();

            if (hovered && ImGui.IsMouseDragging(ImGuiMouseButton.Middle, 0f))
                _viewOffset += ImGui.GetIO().MouseDelta;

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                _linkSource = null;
                _linkFromAny = false;
            }

            var picked = PickNode(origin, mouse);

            if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (_linkSource != null || _linkFromAny)
                {
                    if (picked != null)
                        CreateTransition(picked);

                    _linkSource = null;
                    _linkFromAny = false;
                }
                else if (picked != null)
                {
                    Select(picked);
                    _dragNode = picked;
                    _dragOffset = picked.Position - (mouse - origin - _viewOffset);
                }
                else if (!PickTransition(origin, mouse, anyRect))
                {
                    _selectedState = null;
                    _selectedTransition = null;
                }
            }

            if (_dragNode != null)
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    var next = mouse - origin - _viewOffset + _dragOffset;

                    if (next != _dragNode.Position)
                    {
                        _dragNode.Position = next;
                        MarkChanged();
                    }
                }
                else
                {
                    _dragNode = null;
                }
            }

            if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                _contextNode = picked;
                _contextIsAny = picked == null && InsideAny(anyRect, mouse);
                ImGui.OpenPopup("AnimatorCanvasContext");
            }

            DrawContextMenu(origin, mouse);
        }

        private void DrawContextMenu(Vector2 origin, Vector2 mouse)
        {
            if (!ImGui.BeginPopup("AnimatorCanvasContext"))
                return;

            if (_contextIsAny)
            {
                if (ImGui.MenuItem(Loc.T("animator.makeTransition")))
                {
                    _linkFromAny = true;
                    _linkSource = null;
                }
            }
            else if (_contextNode != null)
            {
                if (ImGui.MenuItem(Loc.T("animator.makeTransition")))
                {
                    _linkSource = _contextNode;
                    _linkFromAny = false;
                }

                if (ImGui.MenuItem(Loc.T("animator.setDefault"), string.Empty, _graph.DefaultState == _contextNode.Name))
                {
                    _graph.DefaultState = _contextNode.Name;
                    MarkChanged();
                }

                if (ImGui.MenuItem(Loc.T("animator.deleteState")))
                {
                    RemoveState(_contextNode);
                    _contextNode = null;
                }
            }
            else
            {
                if (ImGui.MenuItem(Loc.T("animator.addState")))
                {
                    var state = new AnimatorStateData
                    {
                        Name = _graph.UniqueStateName("New State"),
                        Position = mouse - origin - _viewOffset,
                    };

                    _graph.States.Add(state);

                    if (string.IsNullOrEmpty(_graph.DefaultState))
                        _graph.DefaultState = state.Name;

                    Select(state);
                    MarkChanged();
                }
            }

            ImGui.EndPopup();
        }

        private void CreateTransition(AnimatorStateData target)
        {
            var transition = new AnimatorTransitionData { To = target.Name };

            if (_linkFromAny)
            {
                _graph.AnyTransitions.Add(transition);
                _transitionOwner = null;
            }
            else
            {
                if (_linkSource == target)
                    return;

                _linkSource.Transitions.Add(transition);
                _transitionOwner = _linkSource;
            }

            _selectedState = null;
            _selectedTransition = transition;
            MarkChanged();
        }

        private void RemoveState(AnimatorStateData state)
        {
            _graph.States.Remove(state);
            _graph.AnyTransitions.RemoveAll(x => x.To == state.Name);

            foreach (var other in _graph.States)
                other.Transitions.RemoveAll(x => x.To == state.Name);

            if (_graph.DefaultState == state.Name)
                _graph.DefaultState = _graph.States.Count > 0 ? _graph.States[0].Name : string.Empty;

            if (_selectedState == state)
                _selectedState = null;

            _selectedTransition = null;
            MarkChanged();
        }

        private void Select(AnimatorStateData state)
        {
            _selectedState = state;
            _selectedTransition = null;
            _transitionOwner = null;
        }

        private AnimatorStateData PickNode(Vector2 origin, Vector2 mouse)
        {
            for (int i = _graph.States.Count - 1; i >= 0; i--)
            {
                var state = _graph.States[i];
                var min = origin + state.Position + _viewOffset;

                if (mouse.X >= min.X && mouse.X <= min.X + NodeSize.X &&
                    mouse.Y >= min.Y && mouse.Y <= min.Y + NodeSize.Y)
                    return state;
            }

            return null;
        }

        private bool InsideAny(Vector2 anyRect, Vector2 mouse)
        {
            return mouse.X >= anyRect.X && mouse.X <= anyRect.X + NodeSize.X &&
                   mouse.Y >= anyRect.Y && mouse.Y <= anyRect.Y + NodeSize.Y;
        }

        private bool PickTransition(Vector2 origin, Vector2 mouse, Vector2 anyRect)
        {
            foreach (var state in _graph.States)
            {
                var from = origin + state.Position + _viewOffset + NodeSize * 0.5f;

                foreach (var transition in state.Transitions)
                {
                    var target = _graph.Find(transition.To);

                    if (target == null)
                        continue;

                    var to = origin + target.Position + _viewOffset + NodeSize * 0.5f;
                    var lane = Lane(from, to);

                    if (DistanceToSegment(mouse, lane.From, lane.To) <= HitRadius)
                    {
                        _selectedState = null;
                        _selectedTransition = transition;
                        _transitionOwner = state;
                        return true;
                    }
                }
            }

            var anyCenter = anyRect + NodeSize * 0.5f;

            foreach (var transition in _graph.AnyTransitions)
            {
                var target = _graph.Find(transition.To);

                if (target == null)
                    continue;

                var to = origin + target.Position + _viewOffset + NodeSize * 0.5f;
                var lane = Lane(anyCenter, to);

                if (DistanceToSegment(mouse, lane.From, lane.To) <= HitRadius)
                {
                    _selectedState = null;
                    _selectedTransition = transition;
                    _transitionOwner = null;
                    return true;
                }
            }

            return false;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float lengthSquared = ab.LengthSquared();

            if (lengthSquared < 0.0001f)
                return (point - a).Length();

            float t = Math.Clamp(Vector2.Dot(point - a, ab) / lengthSquared, 0f, 1f);
            return (point - (a + ab * t)).Length();
        }

        private void DrawInspector()
        {
            if (_selectedState != null)
            {
                DrawStateInspector(_selectedState);
                return;
            }

            if (_selectedTransition != null)
            {
                DrawTransitionInspector(_selectedTransition);
                return;
            }

            ImGui.TextDisabled(Loc.T("animator.selectHint"));
        }

        private void DrawStateInspector(AnimatorStateData state)
        {
            ImGui.TextDisabled(Loc.T("animator.state"));
            ImGui.Separator();

            ImGui.SetNextItemWidth(-70f);
            string name = state.Name ?? string.Empty;

            if (ImGui.InputText(Loc.T("common.name"), ref name, 64) && !string.IsNullOrWhiteSpace(name))
            {
                Rename(state, name);
                MarkChanged();
            }

            ImGui.SetNextItemWidth(-70f);
            var picked = AssetPicker.Draw(Loc.T("animator.clip"), state.Clip, typeof(SpriteAnimation), _context.Workspace);

            if (picked is JsonValue value && value.TryGetValue(out string clipPath))
            {
                state.Clip = clipPath;
                MarkChanged();
            }

            bool isDefault = state.Name == _graph.DefaultState;

            if (ImGui.Checkbox(Loc.T("animator.defaultState"), ref isDefault) && isDefault)
            {
                _graph.DefaultState = state.Name;
                MarkChanged();
            }

            ImGui.Spacing();
            ImGui.SeparatorText(Loc.T("animator.transitions", state.Transitions.Count));

            foreach (var transition in state.Transitions)
            {
                if (ImGui.Selectable($"→ {transition.To}"))
                {
                    _selectedState = null;
                    _selectedTransition = transition;
                    _transitionOwner = state;
                }
            }
        }

        private void DrawTransitionInspector(AnimatorTransitionData transition)
        {
            ImGui.TextDisabled(_transitionOwner == null ? Loc.T("animator.transitionAny") : Loc.T("animator.transitionOf", _transitionOwner.Name));
            ImGui.Separator();

            ImGui.SetNextItemWidth(-70f);

            if (ImGui.BeginCombo(Loc.T("animator.target"), transition.To))
            {
                foreach (var state in _graph.States)
                {
                    if (ImGui.Selectable(state.Name, state.Name == transition.To))
                    {
                        transition.To = state.Name;
                        MarkChanged();
                    }
                }

                ImGui.EndCombo();
            }

            bool hasExitTime = transition.HasExitTime;

            if (ImGui.Checkbox(Loc.T("animator.hasExitTime"), ref hasExitTime))
            {
                transition.HasExitTime = hasExitTime;
                MarkChanged();
            }

            ImGui.Spacing();
            ImGui.SeparatorText(Loc.T("animator.conditions"));

            if (ImGui.Button(Loc.T("animator.addCondition"), new Vector2(-1f, 0f)))
            {
                transition.Conditions.Add(new AnimatorConditionData
                {
                    Parameter = _graph.Parameters.Count > 0 ? _graph.Parameters[0].Name : string.Empty,
                });

                MarkChanged();
            }

            AnimatorConditionData toRemove = null;

            for (int i = 0; i < transition.Conditions.Count; i++)
            {
                var condition = transition.Conditions[i];
                ImGui.PushID(i);

                ImGui.SetNextItemWidth(-1f);

                if (ImGui.BeginCombo("##Parameter", string.IsNullOrEmpty(condition.Parameter) ? Loc.T("animator.noParameter") : condition.Parameter))
                {
                    foreach (var parameter in _graph.Parameters)
                    {
                        if (ImGui.Selectable(parameter.Name, parameter.Name == condition.Parameter))
                        {
                            condition.Parameter = parameter.Name;
                            MarkChanged();
                        }
                    }

                    ImGui.EndCombo();
                }

                ImGui.SetNextItemWidth(110f);

                if (ImGui.BeginCombo("##Mode", condition.Mode.ToString()))
                {
                    foreach (AnimatorConditionMode mode in Enum.GetValues<AnimatorConditionMode>())
                    {
                        if (ImGui.Selectable(mode.ToString(), mode == condition.Mode))
                        {
                            condition.Mode = mode;
                            MarkChanged();
                        }
                    }

                    ImGui.EndCombo();
                }

                if (NeedsThreshold(condition.Mode))
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(-30f);
                    float threshold = condition.Threshold;

                    if (ImGui.DragFloat("##Threshold", ref threshold, 0.1f))
                    {
                        condition.Threshold = threshold;
                        MarkChanged();
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button("X", new Vector2(-1f, 0f)))
                    toRemove = condition;

                ImGui.PopID();
                ImGui.Spacing();
            }

            if (toRemove != null)
            {
                transition.Conditions.Remove(toRemove);
                MarkChanged();
            }

            ImGui.Spacing();
            ImGui.Separator();

            if (ImGui.Button(Loc.T("animator.deleteTransition"), new Vector2(-1f, 0f)))
            {
                if (_transitionOwner == null)
                    _graph.AnyTransitions.Remove(transition);
                else
                    _transitionOwner.Transitions.Remove(transition);

                _selectedTransition = null;
                MarkChanged();
            }
        }

        private static bool NeedsThreshold(AnimatorConditionMode mode)
        {
            return mode != AnimatorConditionMode.If && mode != AnimatorConditionMode.IfNot;
        }

        private void Rename(AnimatorStateData state, string name)
        {
            string old = state.Name;
            state.Name = name;

            if (_graph.DefaultState == old)
                _graph.DefaultState = name;

            foreach (var other in _graph.States)
            {
                foreach (var transition in other.Transitions)
                {
                    if (transition.To == old)
                        transition.To = name;
                }
            }

            foreach (var transition in _graph.AnyTransitions)
            {
                if (transition.To == old)
                    transition.To = name;
            }
        }

        private void MarkChanged()
        {
            _dirty = true;
        }
    }
}
