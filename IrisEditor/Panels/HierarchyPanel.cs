using Hexa.NET.ImGui;
using IrisEditor.Data;

namespace IrisEditor.Panels
{
    internal sealed class HierarchyPanel : EditorPanel
    {
        private readonly EditorContext _context;

        public HierarchyPanel(EditorContext context)
        {
            _context = context;
        }

        public override string Title => "하이어라키";

        protected override void OnGui()
        {
            if (_context.Scene.Actors.Count == 0)
                ImGui.TextDisabled("우클릭으로 액터를 만드세요");

            ActorData toRemove = null;

            foreach (var actor in _context.Scene.Actors)
            {
                if (ImGui.Selectable($"{actor.Name}##{actor.Id}", _context.Selected == actor))
                    _context.Selected = actor;

                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    _context.Selected = actor;

                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("삭제"))
                        toRemove = actor;

                    ImGui.EndPopup();
                }
            }

            if (toRemove != null)
                _context.RemoveActor(toRemove);

            if (ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                _context.Selected = null;

            if (ImGui.BeginPopupContextWindow("HierarchyContext", ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
            {
                if (ImGui.MenuItem("빈 액터 만들기"))
                    _context.CreateActor();

                ImGui.EndPopup();
            }
        }
    }
}
