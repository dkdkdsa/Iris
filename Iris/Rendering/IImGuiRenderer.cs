using Hexa.NET.ImGui;

namespace Iris.Rendering
{
    /// <summary>
    /// ImGui 드로우 데이터를 그릴 수 있는 백엔드가 선택적으로 구현하는 능력 인터페이스.
    /// 이걸 분리해둔 덕에 <see cref="IRenderBackend"/> 는 ImGui를 몰라도 되고,
    /// 지원하지 않는 백엔드에서는 엔진이 ImGui 시스템 자체를 만들지 않는다.
    /// </summary>
    public interface IImGuiRenderer
    {
        public void RenderDrawData(ImDrawDataPtr dd);
    }
}
