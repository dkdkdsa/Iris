using Hexa.NET.ImGui;

namespace IrisEditor.Panels
{
    public sealed class HierarchyPanel : EditorPanel
    {
        public override string Title => "하이어라키";

        protected override void OnGui()
        {
            ImGui.TextDisabled("(씬 없음)");
        }
    }
}
