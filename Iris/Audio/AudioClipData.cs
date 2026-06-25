using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Audio
{
    public sealed class AudioClipData
    {
        public float[] Samples { get; init; }

        public int SampleRate { get; init; }

        public int Channels { get; init; }

        public int FrameCount => Channels <= 0 ? 0 : Samples.Length / Channels;

        public float DurationSeconds => SampleRate <= 0 ? 0f : (float)FrameCount / SampleRate;
    }
}