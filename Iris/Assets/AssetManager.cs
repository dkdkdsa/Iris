using Iris.Debugging;
using Iris.Files;
using System;
using System.Collections.Generic;

namespace Iris.Assets
{
    public static class AssetManager
    {
        private readonly record struct AssetKey(Type Type, string Path);

        private sealed class Entry
        {
            public IAsset Asset;
            public AssetKey[] Dependencies;
            public int PinCount;
        }

        private static readonly Dictionary<AssetKey, Entry> _entries = new();
        private static readonly Dictionary<object, HashSet<AssetKey>> _scopes = new();
        private static readonly HashSet<AssetKey> _globalRoots = new();
        private static readonly Dictionary<string, string> _keyCache = new(StringComparer.Ordinal);

        private static readonly Stack<List<AssetKey>> _loading = new();
        private static readonly Stack<List<AssetKey>> _framePool = new();

        private static readonly HashSet<AssetKey> _live = new();
        private static readonly Stack<AssetKey> _pendingMarks = new();
        private static readonly List<AssetKey> _doomed = new();

        internal static object CurrentScope { get; set; }

        public static int CachedCount => _entries.Count;

        public static IEnumerable<(Type Type, string Path, IAsset Asset)> Cached
        {
            get
            {
                foreach (var pair in _entries)
                    yield return (pair.Key.Type, pair.Key.Path, pair.Value.Asset);
            }
        }

        public static void Initialize()
        {
            AssetAPI.ActiveAPI = new InternalAssetAPI();
            AssetAPI.ActiveAPI.Init();
        }

        public static T Load<T>(string path) where T : IAsset
        {
            var key = new AssetKey(typeof(T), ResolveKey(path));

            if (_entries.TryGetValue(key, out var cached))
            {
                Attribute(key);
                return (T)cached.Asset;
            }

            var frame = _framePool.Count > 0 ? _framePool.Pop() : new List<AssetKey>();
            _loading.Push(frame);

            IAsset asset;
            bool threw = false;

            try
            {
                asset = AssetAPI.ActiveAPI.LoadAsset<T>(path);
            }
            catch (Exception ex)
            {
                Debug.LogExceptionOnce($"Failed to load asset ({typeof(T).Name}: {path})", ex);

                asset = null;
                threw = true;
            }
            finally
            {
                _loading.Pop();
            }

            var dependencies = frame.Count > 0 ? frame.ToArray() : null;

            frame.Clear();
            _framePool.Push(frame);

            if (asset == null)
            {
                if (!threw)
                    Debug.LogOnce(LogLevel.Warning, $"No loader produced an asset ({typeof(T).Name}: {path})");

                return default;
            }

            _entries[key] = new Entry { Asset = asset, Dependencies = dependencies };

            Attribute(key);
            return (T)asset;
        }

        private static string ResolveKey(string path)
        {
            if (_keyCache.TryGetValue(path, out var key))
                return key;

            key = VirtualFileSystem.GetCacheKey(path);
            _keyCache[path] = key;

            return key;
        }

        public static bool Pin(IAsset asset)
        {
            var entry = Find(asset);

            if (entry == null)
                return false;

            entry.PinCount++;
            return true;
        }

        public static bool Unpin(IAsset asset)
        {
            var entry = Find(asset);

            if (entry == null)
                return false;

            if (entry.PinCount > 0)
                entry.PinCount--;

            return true;
        }

        private static Entry Find(IAsset asset)
        {
            if (asset == null)
                return null;

            foreach (var pair in _entries)
            {
                if (ReferenceEquals(pair.Value.Asset, asset))
                    return pair.Value;
            }

            return null;
        }

        public static int UnloadUnused()
        {
            _live.Clear();
            _pendingMarks.Clear();
            _doomed.Clear();

            foreach (var key in _globalRoots)
            {
                if (_live.Add(key))
                    _pendingMarks.Push(key);
            }

            foreach (var scope in _scopes.Values)
            {
                foreach (var key in scope)
                {
                    if (_live.Add(key))
                        _pendingMarks.Push(key);
                }
            }

            foreach (var pair in _entries)
            {
                if (pair.Value.PinCount > 0 && _live.Add(pair.Key))
                    _pendingMarks.Push(pair.Key);
            }

            while (_pendingMarks.Count > 0)
            {
                if (!_entries.TryGetValue(_pendingMarks.Pop(), out var entry) || entry.Dependencies == null)
                    continue;

                var dependencies = entry.Dependencies;

                for (int i = 0; i < dependencies.Length; i++)
                {
                    if (_live.Add(dependencies[i]))
                        _pendingMarks.Push(dependencies[i]);
                }
            }

            foreach (var pair in _entries)
            {
                if (!_live.Contains(pair.Key))
                    _doomed.Add(pair.Key);
            }

            for (int i = 0; i < _doomed.Count; i++)
                Evict(_doomed[i]);

            return _doomed.Count;
        }

        public static int Unload(string path)
        {
            string cacheKey = ResolveKey(path);

            foreach (var scope in _scopes.Values)
                scope.RemoveWhere(key => key.Path == cacheKey);

            _globalRoots.RemoveWhere(key => key.Path == cacheKey);

            foreach (var pair in _entries)
            {
                if (pair.Key.Path == cacheKey)
                    pair.Value.PinCount = 0;
            }

            return UnloadUnused();
        }

        public static void UnloadAll()
        {
            _scopes.Clear();
            _globalRoots.Clear();
            _loading.Clear();
            _keyCache.Clear();
            CurrentScope = null;

            if (_entries.Count == 0)
                return;

            foreach (var pair in _entries)
                Dispose(pair.Value.Asset);

            _entries.Clear();
        }

        internal static void OpenScope(object owner)
        {
            if (owner == null)
                return;

            if (!_scopes.ContainsKey(owner))
                _scopes[owner] = new HashSet<AssetKey>();

            CurrentScope = owner;
        }

        internal static void ReleaseScope(object owner)
        {
            if (owner == null)
                return;

            _scopes.Remove(owner);

            if (ReferenceEquals(CurrentScope, owner))
                CurrentScope = null;
        }

        private static void Attribute(AssetKey key)
        {
            if (_loading.Count > 0)
            {
                var frame = _loading.Peek();

                if (!frame.Contains(key))
                    frame.Add(key);

                return;
            }

            if (CurrentScope == null)
            {
                _globalRoots.Add(key);
                return;
            }

            if (!_scopes.TryGetValue(CurrentScope, out var roots))
            {
                roots = new HashSet<AssetKey>();
                _scopes[CurrentScope] = roots;
            }

            roots.Add(key);
        }

        private static void Evict(AssetKey key)
        {
            if (!_entries.TryGetValue(key, out var entry))
                return;

            _entries.Remove(key);

            Dispose(entry.Asset);
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
