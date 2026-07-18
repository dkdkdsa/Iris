using Hexa.NET.ImGui;

namespace IrisEditor.Panels
{
    public sealed class ProjectPanel : EditorPanel
    {
        public override string Title => "프로젝트";

        protected override void OnGui()
        {
            ImGui.TextDisabled("(프로젝트 폴더 없음)");
        }
    }
}
