using Iris.Debugging;
using System;

namespace Iris.Core
{
    public sealed class SceneSystem : SystemBase
    {
        public Scene ActiveScene { get; private set; }

        private Scene _pendingScene;
        private bool _hasPending;

        public void LoadScene(Scene scene)
        {
            if (ActiveScene == null)
            {
                ActiveScene = scene;
                Assets.AssetManager.OpenScope(scene);
                return;
            }

            if (_hasPending && _pendingScene != scene)
                _pendingScene?.Dispose();

            _pendingScene = scene;
            _hasPending = true;
        }

        public override void Update()
        {
            ApplyPending();

            try
            {
                ActiveScene?.Update();
            }
            catch (Exception ex)
            {
                Debug.LogExceptionOnce(ex);
            }
        }

        public override void FixedUpdate()
        {
            ApplyPending();

            try
            {
                ActiveScene?.FixedUpdate();
            }
            catch (Exception ex)
            {
                Debug.LogExceptionOnce(ex);
            }
        }

        public override void LateUpdate()
        {
            ApplyPending();

            try
            {
                ActiveScene?.LateUpdate();
            }
            catch (Exception ex)
            {
                Debug.LogExceptionOnce(ex);
            }
        }

        private void ApplyPending()
        {
            if (!_hasPending)
                return;

            var next = _pendingScene;
            var previous = ActiveScene;

            _pendingScene = null;
            _hasPending = false;

            previous?.Dispose();
            ActiveScene = next;

            Assets.AssetManager.OpenScope(next);

            if (previous == null)
                return;

            Assets.AssetManager.ReleaseScope(previous);
            Assets.AssetManager.UnloadUnused();
        }

        public override void Dispose()
        {
            try
            {
                _pendingScene?.Dispose();
                _pendingScene = null;
                _hasPending = false;

                ActiveScene?.Dispose();
                ActiveScene = null;
            }
            catch (Exception ex)
            {
                Debug.LogExceptionOnce(ex);
            }
        }
    }
}
