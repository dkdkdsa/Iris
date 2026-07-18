using Hexa.NET.ImGui;

namespace IrisEditor.Panels
{
    public sealed class InspectorPanel : EditorPanel
    {
        public override string Title => "인스펙터";

        protected override void OnGui()
        {
            ImGui.TextDisabled("(선택된 액터 없음)");
        }
    }
}
