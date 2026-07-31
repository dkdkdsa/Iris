using Iris.Assets;
using Iris.Core;
using Silk.NET.Maths;

namespace Iris.Rendering
{
    public struct RenderCommand
    {
        public Color? color;

        public ITexture texture;
        public Rectangle<int>? src;
        public Rectangle<float> dest;
        public float rotation;
        public int order;
        public bool flipX;
        public bool flipY;

        public bool screenSpace;

        internal int sequence;
    }
}