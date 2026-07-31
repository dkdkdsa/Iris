using Hexa.NET.ImGui;
using Iris.Core;
using IrisEditor.Data;
using IrisEditor.Workspace;
using System;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;
using IrisEditor.Localization;

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

        public override string Title => Loc.Window("panel.inspector");

        protected override void OnGui()
        {
            var actor = _context.Selected;
            if (actor == null)
            {
                ImGui.TextDisabled(Loc.T("inspector.noSelection"));
                return;
            }

            DrawIdentity(actor);

            ImGui.Separator();

            ComponentData toRemove = null;

            foreach (var comp in actor.Components)
            {
                ImGui.PushID(comp.Id.ToString());

                string title = comp.TargetType?.Name ?? (comp.TypeName != null ? Loc.T("inspector.unresolved", comp.TypeName) : Loc.T("inspector.unknownComponent"));
                bool open = ImGui.CollapsingHeader(title, ImGuiTreeNodeFlags.DefaultOpen);

                if (ImGui.BeginPopupContextItem("ComponentContext"))
                {
                    bool removable = comp.TargetType != typeof(Transform);

                    if (ImGui.MenuItem(removable ? Loc.T("common.delete") : Loc.T("inspector.deleteBlocked"), string.Empty, false, removable))
                        toRemove = comp;

                    ImGui.EndPopup();
                }

                if (open)
                {
                    DrawProperties(comp);

                    if (comp.TargetType == typeof(Tilemap) && ImGui.Button(Loc.T("inspector.openTilePalette"), new Vector2(-1f, 0f)))
                        _tilePanel.Open();
                }

                ImGui.PopID();
            }

            if (toRemove != null)
                _context.RemoveComponent(actor, toRemove);

            ImGui.Spacing();

            if (ImGui.Button(Loc.T("inspector.addComponent"), new Vector2(-1f, 0f)))
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

        private void DrawIdentity(ActorData actor)
        {
            var style = ImGui.GetStyle();

            float labels = ImGui.CalcTextSize(Loc.T("inspector.name")).X + ImGui.CalcTextSize(Loc.T("inspector.tag")).X;
            float overhead = labels + style.ItemInnerSpacing.X * 2f + style.ItemSpacing.X;
            float field = MathF.Max((ImGui.GetContentRegionAvail().X - overhead) * 0.5f, 40f);

            ImGui.SetNextItemWidth(field);
            string name = actor.Name ?? string.Empty;

            if (ImGui.InputText(Loc.T("inspector.name"), ref name, 128))
            {
                actor.Name = name;
                _context.MarkDirty();
            }

            ImGui.SameLine();

            ImGui.SetNextItemWidth(field);
            string tag = actor.Tag ?? string.Empty;

            if (ImGui.InputText(Loc.T("inspector.tag"), ref tag, 128))
            {
                actor.Tag = tag;
                _context.MarkDirty();
            }
        }

        private void DrawProperties(ComponentData comp)
        {
            if (comp.Properties is not JsonObject obj || obj.Count == 0)
            {
                ImGui.TextDisabled(Loc.T("common.noData"));
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
