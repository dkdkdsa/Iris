using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Assets
{
    public static class AssetManager
    {
        private static Dictionary<string, IAsset> _assetContainer = new();

        /// <summary>Engine 없이(에디터 등) 에셋 로딩만 쓸 때의 초기화 진입점.</summary>
        public static void Initialize()
        {
            AssetAPI.ActiveAPI = new InternalAssetAPI();
            AssetAPI.ActiveAPI.Init();
        }

        public static T Load<T>(string path) where T : IAsset
        {
            if (_assetContainer.ContainsKey(path))
            {
                return (T)_assetContainer[path];
            }

            var asset = AssetAPI.ActiveAPI.LoadAsset<T>(path);
            _assetContainer.Add(path, asset);

            return asset;
        }

        public static void Unload(string path)
        {
            if (_assetContainer.ContainsKey(path))
            {
                _assetContainer[path].Dispose();
                _assetContainer.Remove(path);
            }
        }
    }
}
