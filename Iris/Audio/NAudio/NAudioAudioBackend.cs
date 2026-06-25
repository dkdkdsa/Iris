using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Iris.Assets;
using Iris.Assets.NAudio;
using Iris.Core;
using System.Collections.Generic;

namespace Iris.Audio.NAudio
{

    public sealed class NAudioAudioBackend : IAudioBackend, IFactory<IAudioClip, AudioClipData>
    {
        private const int MixSampleRate = 44100;
        private const int MixChannels = 2;

        private readonly WaveFormat _mixFormat = WaveFormat.CreateIeeeFloatWaveFormat(MixSampleRate, MixChannels);

        private readonly object _gate = new object();
        private readonly Dictionary<int, ISampleProvider> _voices = new Dictionary<int, ISampleProvider>();

        private WaveOutEvent _output;
        private MixingSampleProvider _mixer;
        private int _nextVoiceId;

        public void Init()
        {
            _mixer = new MixingSampleProvider(_mixFormat)
            {
                ReadFully = true
            };
            _mixer.MixerInputEnded += OnMixerInputEnded;

            _output = new WaveOutEvent
            {
                DesiredLatency = 100
            };
            _output.Init(_mixer);
            _output.Play();
        }

        public int Play(IAudioClip clip, float volume, bool loop)
        {
            if (clip is not NAudioAudioClip naudioClip)
                return 0;

            var source = new ClipSampleProvider(naudioClip.Samples, naudioClip.Format, loop);
            var voice = new VolumeSampleProvider(source)
            {
                Volume = volume
            };

            int id;
            lock (_gate)
            {
                id = ++_nextVoiceId;
                _voices[id] = voice;
            }

            _mixer.AddMixerInput(voice);
            return id;
        }

        public void Stop(int id)
        {
            ISampleProvider voice;
            lock (_gate)
            {
                if (!_voices.Remove(id, out voice))
                    return;
            }

            _mixer.RemoveMixerInput(voice);
        }

        public IAudioClip Create(AudioClipData request)
        {
            float[] converted = ConvertToMixFormat(request);
            return new NAudioAudioClip(converted, _mixFormat, request.DurationSeconds);
        }

        private float[] ConvertToMixFormat(AudioClipData data)
        {
            var srcFormat = WaveFormat.CreateIeeeFloatWaveFormat(data.SampleRate, data.Channels);
            ISampleProvider provider = new ClipSampleProvider(data.Samples, srcFormat, loop: false);

            if (provider.WaveFormat.Channels == 1 && MixChannels == 2)
                provider = new MonoToStereoSampleProvider(provider);
            else if (provider.WaveFormat.Channels == 2 && MixChannels == 1)
                provider = new StereoToMonoSampleProvider(provider);

            if (provider.WaveFormat.SampleRate != MixSampleRate)
                provider = new WdlResamplingSampleProvider(provider, MixSampleRate);

            return Drain(provider);
        }

        private static float[] Drain(ISampleProvider provider)
        {
            var result = new List<float>();
            float[] buffer = new float[provider.WaveFormat.SampleRate * provider.WaveFormat.Channels];

            int read;
            while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                    result.Add(buffer[i]);
            }

            return result.ToArray();
        }

        private void OnMixerInputEnded(object sender, SampleProviderEventArgs e)
        {
            lock (_gate)
            {
                foreach (var pair in _voices)
                {
                    if (ReferenceEquals(pair.Value, e.SampleProvider))
                    {
                        _voices.Remove(pair.Key);
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_output != null)
            {
                _output.Stop();
                _output.Dispose();
                _output = null;
            }

            if (_mixer != null)
            {
                _mixer.MixerInputEnded -= OnMixerInputEnded;
                _mixer.RemoveAllMixerInputs();
                _mixer = null;
            }

            lock (_gate)
            {
                _voices.Clear();
            }
        }
    }
}
