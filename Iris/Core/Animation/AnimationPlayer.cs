using System;

namespace Iris.Core
{
    public sealed class AnimationPlayer : Component
    {
        private AnimationClip _active;
        private Component[] _targets;
        private float _time;

        public AnimationClip Clip { get; set; }
        public float Speed { get; set; } = 1f;
        public bool PlayOnStart { get; set; } = true;

        public bool IsPlaying { get; private set; }
        public float Time => _time;

        public event Action OnComplete;

        public void Play(AnimationClip clip)
        {
            Clip = clip;
            Play();
        }

        public void Play()
        {
            Rebind(Clip);

            _time = 0f;
            IsPlaying = _active != null && _active.Tracks.Count > 0;

            Sample(0f);
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public void Resume()
        {
            IsPlaying = _active != null && _active.Tracks.Count > 0;
        }

        public void Stop()
        {
            IsPlaying = false;
            _time = 0f;
        }

        public void Sample(float time)
        {
            if (_active == null || _targets == null)
                return;

            var tracks = _active.Tracks;

            for (int i = 0; i < tracks.Count; i++)
            {
                if (_targets[i] != null)
                    tracks[i].Apply(_targets[i], time);
            }
        }

        protected override void Awake()
        {
            if (PlayOnStart && Clip != null)
                Play();
            else
                Rebind(Clip);
        }

        public override void Update()
        {
            if (Clip != _active)
            {
                Rebind(Clip);

                _time = 0f;
                IsPlaying = PlayOnStart && _active != null && _active.Tracks.Count > 0;
            }

            if (!IsPlaying || _active == null)
                return;

            float length = _active.Length;

            if (length <= 0f)
            {
                Sample(0f);
                return;
            }

            _time += Iris.Core.Time.DeltaTime * Speed;

            if (_time >= length)
            {
                if (_active.Loop)
                {
                    _time %= length;
                }
                else
                {
                    _time = length;
                    IsPlaying = false;

                    Sample(_time);
                    OnComplete?.Invoke();
                    return;
                }
            }
            else if (_time < 0f)
            {
                _time = _active.Loop ? length + _time % length : 0f;
            }

            Sample(_time);
        }

        private void Rebind(AnimationClip clip)
        {
            _active = clip;

            if (clip == null || OwnerActor == null)
            {
                _targets = null;
                return;
            }

            var tracks = clip.Tracks;
            _targets = new Component[tracks.Count];

            for (int i = 0; i < tracks.Count; i++)
            {
                var type = RuntimeTypeResolver.ResolveComponent(tracks[i].ComponentType);

                if (type == null)
                    continue;

                _targets[i] = OwnerActor.GetComponent(type);
            }
        }
    }
}
