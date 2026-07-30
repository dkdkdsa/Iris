using Iris.Files;
using System;
using System.Collections.Generic;

namespace Iris.Assets
{
    public static class AssetManager
    {
        private static Dictionary<(Type Type, string Path), IAsset> _assetContainer = new();

        public static void Initialize()
        {
            AssetAPI.ActiveAPI = new InternalAssetAPI();
            AssetAPI.ActiveAPI.Init();
        }

        public static T Load<T>(string path) where T : IAsset
        {
            var key = (typeof(T), VirtualFileSystem.Canonicalize(path));

            if (_assetContainer.TryGetValue(key, out var cached))
                return (T)cached;

            var asset = AssetAPI.ActiveAPI.LoadAsset<T>(path);
            _assetContainer.Add(key, asset);

            return asset;
        }

        public static void Unload(string path)
        {
            string canonical = VirtualFileSystem.Canonicalize(path);
            var keys = new List<(Type Type, string Path)>();

            foreach (var pair in _assetContainer)
            {
                if (pair.Key.Path == canonical)
                    keys.Add(pair.Key);
            }

            foreach (var key in keys)
            {
                _assetContainer[key]?.Dispose();
                _assetContainer.Remove(key);
            }
        }
    }
}
