using Hexa.NET.ImGui;

namespace IrisEditor.Panels
{
    public sealed class ScenePanel : EditorPanel
    {
        public override string Title => "씬";

        protected override void OnGui()
        {
            ImGui.TextDisabled("(씬 미리보기가 여기 그려진다)");
        }
    }
}
