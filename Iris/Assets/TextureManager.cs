using Iris.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Iris.Assets
{
    public sealed class TextureManager : IDisposable
    {
        private readonly ITextureFactory _factory;
        private readonly IImageDecoder _decoder;
        private readonly Dictionary<string, ITexture> _cache = new();

        internal TextureManager(ITextureFactory factory, IImageDecoder decoder)
        {
            _factory = factory;
            _decoder = decoder;
        }

        public ITexture Create(int width, int height)
        {
            return _factory.CreateTexture(width, height);
        }

        public ITexture Load(string path)
        {
            if (_cache.TryGetValue(path, out var cached))
                return cached;

            var bytes = File.ReadAllBytes(path);
            var img = _decoder.Decode(bytes);

            var tex = _factory.CreateTexture(img.width, img.height);
            tex.UpdateTexture(MemoryMarshal.Cast<byte, Color>(img.pixels));

            _cache[path] = tex;
            return tex;
        }

        public void Unload(string path)
        {
            if (_cache.Remove(path, out var tex))
                tex.Dispose();
        }

        public void Dispose()
        {
            foreach (var tex in _cache.Values)
                tex.Dispose();
            _cache.Clear();
        }
    }
}
