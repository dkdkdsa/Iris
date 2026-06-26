using Iris.Assets;
using Silk.NET.Maths;

namespace Iris.Rendering
{
    public struct RenderCommand
    {
        public ITexture texture;
        public Rectangle<int>? src;
        public Rectangle<float> dest;
        public float rotation;
        public int order;
        public bool flipX;
        public bool flipY;
    }
}