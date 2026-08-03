using Iris.Assets;
using System;
using System.Collections.Generic;

namespace Iris.Core
{
    public sealed class AnimationClip : IAsset
    {
        private readonly AnimationTrack[] _tracks;

        public string Name { get; }
        public int SampleRate { get; }
        public bool Loop { get; set; }
        public float Length { get; }

        public IReadOnlyList<AnimationTrack> Tracks => _tracks;

        public AnimationClip(string name, AnimationTrack[] tracks, int sampleRate = 12, bool loop = true, float length = 0f)
        {
            Name = name ?? string.Empty;
            _tracks = tracks ?? Array.Empty<AnimationTrack>();
            SampleRate = sampleRate > 0 ? sampleRate : 12;
            Loop = loop;

            float longest = 0f;

            for (int i = 0; i < _tracks.Length; i++)
            {
                if (_tracks[i].Duration > longest)
                    longest = _tracks[i].Duration;
            }

            Length = length > 0f ? length : longest;
        }

        public void Dispose()
        {
        }
    }
}
