using Iris.Audio;
using Iris.Core;
using Iris.Files;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace Iris.Assets
{
    internal class NAudioAudioLoader : IAssetLoader
    {
        public IAsset LoadAsset(string path)
        {
            using var stream = new MemoryStream(VirtualFileSystem.ReadAllBytes(path));
            using var reader = CreateReader(path, stream);

            var sampleProvider = reader.ToSampleProvider();
            int sampleRate = sampleProvider.WaveFormat.SampleRate;
            int channels = sampleProvider.WaveFormat.Channels;
            var samples = new List<float>();

            float[] buffer = new float[sampleRate * channels];

            int read;
            while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                    samples.Add(buffer[i]);
            }

            return Factories.Create<IAudioClip, AudioClipData>(new AudioClipData
            {
                Channels = channels,
                SampleRate = sampleRate,
                Samples = samples.ToArray()
            });
        }

        private static WaveStream CreateReader(string path, Stream stream)
        {
            string ext = Path.GetExtension(path);

            if (ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                return new Mp3FileReader(stream);

            return new WaveFileReader(stream);
        }
    }
}
