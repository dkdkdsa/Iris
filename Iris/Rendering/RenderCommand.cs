using Iris.Assets;
using Iris.Core;
using Silk.NET.Maths;

namespace Iris.Rendering
{
    public struct RenderCommand
    {
        /// <summary>틴트 색. null이면 원본 그대로(흰색 255 모드).</summary>
        public Color? color;

        public ITexture texture;
        public Rectangle<int>? src;
        public Rectangle<float> dest;
        public float rotation;
        public int order;
        public bool flipX;
        public bool flipY;

        /// <summary>true면 dest를 카메라 변환 없이 화면 픽셀 좌표로 그린다(UI용).</summary>
        public bool screenSpace;
    }
}