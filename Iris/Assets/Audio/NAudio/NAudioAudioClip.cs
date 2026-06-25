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
            // 관리되는 PCM 버퍼라 별도 해제 리소스 없음.
            // (FMOD라면 여기서 Sound.release() 호출)
        }
    }
}
