using System;
using NAudio.Wave;

namespace Iris.Audio.NAudio
{

    internal sealed class ClipSampleProvider : ISampleProvider
    {
        private readonly float[] _data;
        private readonly bool _loop;
        private int _position;

        public ClipSampleProvider(float[] data, WaveFormat format, bool loop)
        {
            _data = data;
            WaveFormat = format;
            _loop = loop;
            _position = 0;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int written = 0;

            while (written < count)
            {
                int remaining = _data.Length - _position;

                if (remaining <= 0)
                {
                    if (_loop)
                    {
                        _position = 0;
                        if (_data.Length == 0)
                            break;
                        continue;
                    }

                    break;
                }

                int toCopy = Math.Min(remaining, count - written);
                Array.Copy(_data, _position, buffer, offset + written, toCopy);

                _position += toCopy;
                written += toCopy;
            }

            return written;
        }
    }
}
