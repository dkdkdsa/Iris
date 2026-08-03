using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace IrisEditor.Data
{
    internal enum AnimationTrackKind
    {
        Sprite,
        Float,
        Vector2,
        Color,
    }

    internal sealed class AnimationKeyData
    {
        public float Time;
        public JsonNode Value;
    }

    internal sealed class AnimationTrackData
    {
        public string Component = "Iris.Core.SpriteRenderer";
        public string Property = "Sprite";
        public AnimationTrackKind Kind = AnimationTrackKind.Sprite;
        public List<AnimationKeyData> Keys = new();

        public string Label => $"{ShortComponent} : {Property}";

        public string ShortComponent
        {
            get
            {
                int dot = Component.LastIndexOf('.');
                return dot >= 0 && dot + 1 < Component.Length ? Component[(dot + 1)..] : Component;
            }
        }

        public void Sort()
        {
            Keys.Sort(static (a, b) => a.Time.CompareTo(b.Time));
        }
    }

    internal sealed class AnimationClipData
    {
        public int SampleRate = 12;
        public bool Loop = true;
        public List<AnimationTrackData> Tracks = new();

        public float Length
        {
            get
            {
                float longest = 0f;

                foreach (var track in Tracks)
                {
                    foreach (var key in track.Keys)
                    {
                        if (key.Time > longest)
                            longest = key.Time;
                    }
                }

                return longest;
            }
        }
    }
}
