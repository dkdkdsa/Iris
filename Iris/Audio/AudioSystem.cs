using Box2D.NET;
using Iris.Assets;
using Iris.Core;

namespace Iris.Audio
{
    public sealed class AudioSystem : SystemBase
    {
        private readonly IAudioBackend _backend;

        internal AudioSystem(IAudioBackend backend)
        {
            _backend = backend;
        }

        public int Play(IAudioClip clip, float volume = 1f, bool loop = false)
        {
            return _backend.Play(clip, volume, loop);
        }

        public void Stop(int voiceId)
        {
            _backend.Stop(voiceId);
        }

        public override void Dispose()
        {
            _backend.Dispose();
        }
    }
}
