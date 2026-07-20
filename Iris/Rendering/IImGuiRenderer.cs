using Hexa.NET.ImGui;

namespace Iris.Rendering
{
    public interface IImGuiRenderer
    {
        public void RenderDrawData(ImDrawDataPtr dd);
    }
}
