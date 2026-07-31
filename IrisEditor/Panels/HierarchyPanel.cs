using Hexa.NET.ImGui;
using IrisEditor.Data;
using System.Collections.Generic;
using IrisEditor.Localization;

namespace IrisEditor.Panels
{
    internal sealed class HierarchyPanel : EditorPanel
    {
        private const string DragPayload = "IRIS_ACTOR";

        private readonly EditorContext _context;

        private ActorData _dragged;
        private ActorData _toRemove;
        private ActorData _reparentChild;
        private ActorData _reparentTarget;
        private bool _hasReparent;
        private ActorData _saveAsPrefab;

        public HierarchyPanel(EditorContext context)
        {
            _context = context;
        }

        public override string Title => Loc.Window("panel.hierarchy");

        protected override void OnGui()
        {
            var scene = _context.Scene;

            if (scene.Actors.Count == 0)
                ImGui.TextDisabled(Loc.T("hierarchy.emptyHint"));

            _toRemove = null;
            _hasReparent = false;

            for (int i = 0; i < scene.Actors.Count; i++)
            {
                var actor = scene.Actors[i];

                if (actor.ParentId == null || SceneTransforms.FindParent(scene, actor) == null)
                    DrawNode(scene, actor, 0);
            }

            if (_hasReparent)
            {
                SceneTransforms.Reparent(scene, _reparentChild, _reparentTarget);
                _context.MarkDirty();
            }

            if (_saveAsPrefab != null)
            {
                _context.SaveAsPrefab(_saveAsPrefab);
                _saveAsPrefab = null;
            }

            if (_toRemove != null)
                _context.RemoveActor(_toRemove);

            if (ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                _context.Selected = null;

            if (ImGui.BeginPopupContextWindow("HierarchyContext", ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
            {
                if (ImGui.MenuItem(Loc.T("hierarchy.createEmpty")))
                    _context.CreateActor();

                ImGui.EndPopup();
            }
        }

        private void DrawNode(SceneData scene, ActorData actor, int depth)
        {
            if (depth > 64)
                return;

            var children = CollectChildren(scene, actor);

            var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick
                      | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.DefaultOpen;

            if (children.Count == 0)
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

            if (_context.Selected == actor)
                flags |= ImGuiTreeNodeFlags.Selected;

            bool isPrefabInstance = !string.IsNullOrEmpty(actor.PrefabSource);

            if (isPrefabInstance)
                ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFC896);

            bool open = ImGui.TreeNodeEx($"{actor.Name}##{actor.Id}", flags);

            if (isPrefabInstance)
                ImGui.PopStyleColor();

            if ((ImGui.IsItemClicked(ImGuiMouseButton.Left) && !ImGui.IsItemToggledOpen()) ||
                ImGui.IsItemClicked(ImGuiMouseButton.Right))
                _context.Selected = actor;

            if (isPrefabInstance && ImGui.IsItemHovered())
                ImGui.SetTooltip(actor.PrefabSource);

            if (ImGui.BeginDragDropSource())
            {
                _dragged = actor;

                unsafe
                {
                    byte dummy = 0;
                    ImGui.SetDragDropPayload(DragPayload, &dummy, 1);
                }

                ImGui.Text(actor.Name);
                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload(DragPayload);

                if (!payload.IsNull && _dragged != null && _dragged != actor)
                {
                    _reparentChild = _dragged;
                    _reparentTarget = actor;
                    _hasReparent = true;
                }

                ImGui.EndDragDropTarget();
            }

            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.MenuItem(Loc.T("hierarchy.createChild")))
                    _context.CreateActor(actor);

                if (actor.ParentId != null && ImGui.MenuItem(Loc.T("hierarchy.detachParent")))
                {
                    _reparentChild = actor;
                    _reparentTarget = null;
                    _hasReparent = true;
                }

                if (ImGui.MenuItem(Loc.T("hierarchy.saveAsPrefab"), string.Empty, false, _context.Workspace != null))
                    _saveAsPrefab = actor;

                if (ImGui.MenuItem(Loc.T("common.delete")))
                    _toRemove = actor;

                ImGui.EndPopup();
            }

            if (open && children.Count > 0)
            {
                foreach (var child in children)
                    DrawNode(scene, child, depth + 1);

                ImGui.TreePop();
            }
        }

        private static List<ActorData> CollectChildren(SceneData scene, ActorData actor)
        {
            var result = new List<ActorData>();

            foreach (var candidate in scene.Actors)
            {
                if (candidate.ParentId == actor.Id)
                    result.Add(candidate);
            }

            return result;
        }
    }
}
