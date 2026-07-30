using Iris.Core;
using Iris.Files;
using Silk.NET.Maths;
using StbiSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Iris.Assets
{
    internal class StbTextureLoader : IAssetLoader
    {
        public IAsset LoadAsset(string path)
        {
            var bytes = VirtualFileSystem.ReadAllBytes(path);
            var img = Stbi.LoadFromMemory(bytes, 4);

            var tex = Factorys.Create<ITexture, Vector2D<int>>(new (img.Width, img.Height));
            tex.UpdateTexture(MemoryMarshal.Cast<byte, Color>(img.Data));

            return tex;
        }
    }
}
