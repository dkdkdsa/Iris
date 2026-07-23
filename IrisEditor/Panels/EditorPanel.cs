using Hexa.NET.ImGui;

namespace IrisEditor.Panels
{
    public abstract class EditorPanel
    {
        public bool IsOpen = true;

        public abstract string Title { get; }

        protected virtual ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;

        public virtual void Draw()
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
