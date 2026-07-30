using global::NAudio.Wave;

namespace Iris.Assets.NAudio
{

    internal sealed class NAudioAudioClip : IAudioClip
    {
        public float[] Samples { get; }

        public WaveFormat Format { get; }

        public float Duration { get; }

        public NAudioAudioClip(float[] samples, WaveFormat format, float duration)
        {
            Samples = samples;
            Format = format;
            Duration = duration;
        }

        public void Dispose()
        {
        }
    }
}
