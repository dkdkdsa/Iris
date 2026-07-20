using Hexa.NET.ImGui;

namespace IrisEditor
{
    /// <summary>모든 에디터 창의 베이스. Begin/End 수명은 여기서 처리하고 파생은 OnGui만 구현한다.</summary>
    public abstract class EditorPanel
    {
        public bool IsOpen = true;

        /// <summary>창 제목이자 도킹 식별자. DockBuilder가 이 문자열로 창을 찾으므로 바꾸면 레이아웃이 풀린다.</summary>
        public abstract string Title { get; }

        protected virtual ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;

        public void Draw()
        {
            if (!IsOpen)
                return;

            if (ImGui.Begin(Title, ref IsOpen, WindowFlags))
                OnGui();

            ImGui.End();
        }

        protected abstract void OnGui();
    }
}
