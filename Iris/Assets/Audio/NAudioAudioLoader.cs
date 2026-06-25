using Iris.Audio;
using Iris.Core;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Assets
{
    internal class NAudioAudioLoader : IAssetLoader
    {
        public IAsset LoadAsset(string path)
        {
            using (var reader = new AudioFileReader(path))
            {
                int sampleLate = reader.WaveFormat.SampleRate;
                int channels = reader.WaveFormat.Channels;
                var samples = new List<float>();


                float[] buffer = new float[sampleLate * channels];

                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                        samples.Add(buffer[i]);
                }

                return Factorys.Create<IAudioClip, AudioClipData>(new AudioClipData
                {
                    Channels = channels,
                    SampleRate = sampleLate,
                    Samples = samples.ToArray()
                });
            }
        }
    }
}
