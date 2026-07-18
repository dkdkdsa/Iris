using System;

namespace Iris.Rendering
{
    /// <summary>
    /// 백버퍼 대신 그려 넣을 수 있는 오프스크린 대상.
    /// 에셋 텍스처와 달리 로드되는 게 아니라 할당되고, 크기가 바뀌면 새로 만든다.
    /// </summary>
    public interface IRenderTarget : IDisposable
    {
        public int Width { get; }
        public int Height { get; }

        /// <summary>네이티브 텍스처 핸들. ImGui.Image()에 그대로 넘길 수 있다.</summary>
        public nint Handle { get; }
    }
}
