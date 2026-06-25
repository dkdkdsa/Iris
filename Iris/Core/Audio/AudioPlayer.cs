using Iris.Assets;
using Iris.Audio;

namespace Iris.Core
{
    public sealed class AudioPlayer : Component
    {
        private AudioSystem _system;
        private int _id;

        public IAudioClip Clip { get; set; }
        public float Volume { get; set; } = 1;
        public bool Loop { get; set; } = false;
        public bool IsPlaying { get; private set; } = false;

        protected override void OnAttached()
        {
            _system = SystemManager.Instance.GetSystem<AudioSystem>();
        }

        public void Play()
        {
            if (Clip == null)
                return;

            if (IsPlaying)
            {
                Stop();
            }

            _id = _system.Play(Clip, Volume, Loop);
            IsPlaying = true;
        }

        public void Stop()
        {
            if (!IsPlaying)
                return;

            _system.Stop(_id);
        }
    }
}
