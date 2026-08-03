using System;
using System.Text.Json.Nodes;

namespace IrisEditor.Data
{
    internal static class AnimationSampler
    {
        public static JsonNode Evaluate(AnimationTrackData track, float time)
        {
            if (track == null || track.Keys.Count == 0)
                return null;

            int index = FindIndex(track, time);

            if (index < 0)
                return track.Keys[0].Value;

            if (index >= track.Keys.Count - 1)
                return track.Keys[^1].Value;

            var from = track.Keys[index];
            var to = track.Keys[index + 1];

            if (track.Kind == AnimationTrackKind.Sprite)
                return from.Value;

            float span = to.Time - from.Time;
            float amount = span <= 0f ? 1f : (time - from.Time) / span;

            return track.Kind switch
            {
                AnimationTrackKind.Float => JsonValue.Create(Lerp(Number(from.Value), Number(to.Value), amount)),
                AnimationTrackKind.Vector2 => new JsonArray(
                    JsonValue.Create(Lerp(Element(from.Value, 0), Element(to.Value, 0), amount)),
                    JsonValue.Create(Lerp(Element(from.Value, 1), Element(to.Value, 1), amount))),
                AnimationTrackKind.Color => new JsonArray(
                    JsonValue.Create(Channel(from.Value, to.Value, 0, amount)),
                    JsonValue.Create(Channel(from.Value, to.Value, 1, amount)),
                    JsonValue.Create(Channel(from.Value, to.Value, 2, amount)),
                    JsonValue.Create(Channel(from.Value, to.Value, 3, amount))),
                _ => from.Value,
            };
        }

        private static int FindIndex(AnimationTrackData track, float time)
        {
            int low = 0;
            int high = track.Keys.Count - 1;

            while (low <= high)
            {
                int mid = (low + high) >> 1;

                if (track.Keys[mid].Time <= time)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            return high;
        }

        private static float Lerp(float from, float to, float amount)
        {
            return from + (to - from) * amount;
        }

        private static float Channel(JsonNode from, JsonNode to, int index, float amount)
        {
            return MathF.Round(Lerp(Element(from, index, 255f), Element(to, index, 255f), amount));
        }

        private static float Number(JsonNode node, float fallback = 0f)
        {
            return node is JsonValue value && value.TryGetValue(out float f) ? f : fallback;
        }

        private static float Element(JsonNode node, int index, float fallback = 0f)
        {
            return node is JsonArray array && index < array.Count ? Number(array[index], fallback) : fallback;
        }
    }
}
