using Iris.Core;
using System;

namespace Iris.Assets
{
    public interface ITexture : IAsset
    {
        public int Width { get; }
        public int Height { get; }

        /// <summary>네이티브 텍스처 핸들. ImGui.Image()에 그대로 넘길 수 있다.</summary>
        public nint Handle { get; }

        public void UpdateTexture(ReadOnlySpan<Color> data);
    }
}
