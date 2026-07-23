using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Assets
{
    internal class InternalAssetAPI : AssetAPI
    {
        private readonly Dictionary<Type, IAssetLoader> _loaders = new();

        internal override void Init()
        {
            _loaders.Add(typeof(ITexture), new StbTextureLoader());
            _loaders.Add(typeof(IAudioClip), new NAudioAudioLoader());
            _loaders.Add(typeof(IFont), new StbFontLoader());
            _loaders.Add(typeof(Iris.UI.IUILayout), new Iris.UI.UILayoutLoader());
            _loaders.Add(typeof(Iris.Core.SpriteAnimation), new SpriteAnimationLoader());
            _loaders.Add(typeof(Iris.Core.Tile), new TileLoader());
            _loaders.Add(typeof(Iris.Core.Prefab), new PrefabLoader());
        }

        internal override T LoadAsset<T>(string path)
        {
            if (_loaders.TryGetValue(typeof(T), out var loader))
            {
                return (T)loader.LoadAsset(path);
            }

            return default;
        }
    }
}
