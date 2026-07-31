using Iris.Debugging;
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
            var key = (typeof(T), VirtualFileSystem.GetCacheKey(path));

            if (_assetContainer.TryGetValue(key, out var cached))
                return (T)cached;

            var asset = AssetAPI.ActiveAPI.LoadAsset<T>(path);
            _assetContainer.Add(key, asset);

            return asset;
        }

        public static void Unload(string path)
        {
            string cacheKey = VirtualFileSystem.GetCacheKey(path);
            var keys = new List<(Type Type, string Path)>();

            foreach (var pair in _assetContainer)
            {
                if (pair.Key.Path == cacheKey)
                    keys.Add(pair.Key);
            }

            foreach (var key in keys)
            {
                Dispose(_assetContainer[key]);
                _assetContainer.Remove(key);
            }
        }

        public static void UnloadAll()
        {
            if (_assetContainer.Count == 0)
                return;

            foreach (var pair in _assetContainer)
                Dispose(pair.Value);

            _assetContainer.Clear();
        }

        private static void Dispose(IAsset asset)
        {
            if (asset == null)
                return;

            try
            {
                asset.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogExceptionOnce("Failed to unload asset", ex);
            }
        }
    }
}
