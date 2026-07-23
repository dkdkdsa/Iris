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
        private readonly TilePalettePanel _tilePanel;

        public InspectorPanel(EditorContext context, TilePalettePanel tilePanel)
        {
            _context = context;
            _tilePanel = tilePanel;
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
                {
                    DrawProperties(comp);

                    if (comp.TargetType == typeof(Tilemap) && ImGui.Button("타일 팔레트 열기", new Vector2(-1f, 0f)))
                        _tilePanel.Open();
                }

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

            if (PropertyDrawer.Draw(obj, assetProps, _context.Workspace, HiddenMembers.Get(comp.TargetType)))
                _context.MarkDirty();
        }
    }
}
