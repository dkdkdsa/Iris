using Iris.Assets;
using System;

namespace Iris.Audio
{
    public interface IAudioBackend : IDisposable
    {
        public void Init();
        public int Play(IAudioClip clip, float volume, bool loop);
        public void Stop(int id);
    }
}
