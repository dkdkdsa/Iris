using Hexa.NET.ImGui;
using Iris.Core;
using IrisEditor.Data;
using IrisEditor.Workspace;
using System;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;

namespace IrisEditor.Panels
{
    internal sealed class InspectorPanel : EditorPanel
    {
        private readonly EditorContext _context;

        public InspectorPanel(EditorContext context)
        {
            _context = context;
        }

        public override string Title => "인스펙터";

        protected override void OnGui()
        {
            var actor = _context.Selected;
            if (actor == null)
            {
                ImGui.TextDisabled("(선택된 액터 없음)");
                return;
            }

            string name = actor.Name ?? string.Empty;
            if (ImGui.InputText("이름", ref name, 128))
            {
                actor.Name = name;
                _context.MarkDirty();
            }

            ImGui.Separator();

            ComponentData toRemove = null;

            foreach (var comp in actor.Components)
            {
                ImGui.PushID(comp.Id.ToString());

                string title = comp.TargetType?.Name ?? (comp.TypeName != null ? $"{comp.TypeName} (미해석)" : "(알 수 없는 컴포넌트)");
                bool open = ImGui.CollapsingHeader(title, ImGuiTreeNodeFlags.DefaultOpen);

                if (ImGui.BeginPopupContextItem("ComponentContext"))
                {
                    bool removable = comp.TargetType != typeof(Transform);

                    if (ImGui.MenuItem(removable ? "삭제" : "삭제 (Transform은 필수)", string.Empty, false, removable))
                        toRemove = comp;

                    ImGui.EndPopup();
                }

                if (open)
                    DrawProperties(comp);

                ImGui.PopID();
            }

            if (toRemove != null)
                _context.RemoveComponent(actor, toRemove);

            ImGui.Spacing();

            if (ImGui.Button("컴포넌트 추가", new Vector2(-1f, 0f)))
                ImGui.OpenPopup("AddComponentPopup");

            if (ImGui.BeginPopup("AddComponentPopup"))
            {
                foreach (var type in ComponentCatalog.Types)
                {
                    if (type == typeof(Transform))
                        continue;

                    if (ImGui.MenuItem(type.Name))
                        _context.AddComponent(actor, type);
                }

                ImGui.EndPopup();
            }
        }

        private void DrawProperties(ComponentData comp)
        {
            if (comp.Properties is not JsonObject obj || obj.Count == 0)
            {
                ImGui.TextDisabled("(데이터 없음)");
                return;
            }

            var assetProps = comp.TargetType != null
                ? ComponentCatalog.GetAssetProperties(comp.TargetType)
                : null;

            foreach (var (key, value) in obj.ToList())
            {
                var changed = assetProps != null && assetProps.TryGetValue(key, out var assetType)
                    ? DrawAssetProperty(key, value, assetType)
                    : DrawProperty(key, value);

                if (changed != null)
                {
                    obj[key] = changed;
                    _context.MarkDirty();
                }
            }
        }

        private static JsonNode DrawAssetProperty(string label, JsonNode value, Type assetType)
        {
            string current = value is JsonValue v && v.TryGetValue(out string s) ? s : string.Empty;
            JsonNode changed = null;

            string edited = current;
            if (ImGui.InputText(label, ref edited, 256) && edited != current)
                changed = JsonValue.Create(edited);

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload(AssetDragDrop.PayloadType);

                if (!payload.IsNull && AssetDragDrop.Current != null &&
                    assetType.IsAssignableFrom(AssetDragDrop.Current.AssetType))
                    changed = JsonValue.Create(AssetDragDrop.Current.Path);

                ImGui.EndDragDropTarget();
            }

            return changed;
        }

        private static JsonNode DrawProperty(string label, JsonNode value)
        {
            switch (value)
            {
                case JsonValue v when v.TryGetValue(out bool b):
                    if (ImGui.Checkbox(label, ref b))
                        return JsonValue.Create(b);
                    return null;

                case JsonValue v when v.TryGetValue(out float f):
                    if (ImGui.DragFloat(label, ref f, 0.1f))
                        return JsonValue.Create(f);
                    return null;

                case JsonValue v when v.TryGetValue(out string s):
                    if (ImGui.InputText(label, ref s, 256))
                        return JsonValue.Create(s);
                    return null;

                case JsonArray { Count: 2 } arr when TryGetVector2(arr, out var vec):
                    if (ImGui.DragFloat2(label, ref vec, 0.1f))
                        return new JsonArray(vec.X, vec.Y);
                    return null;

                default:
                    ImGui.TextDisabled($"{label}: {value?.ToJsonString() ?? "null"}");
                    return null;
            }
        }

        private static bool TryGetVector2(JsonArray arr, out Vector2 vec)
        {
            vec = default;

            if (arr[0] is JsonValue a && a.TryGetValue(out float x) &&
                arr[1] is JsonValue b && b.TryGetValue(out float y))
            {
                vec = new Vector2(x, y);
                return true;
            }

            return false;
        }
    }
}
