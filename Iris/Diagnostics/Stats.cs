using System;

namespace Iris.Diagnostics
{
    public static class Stats
    {
        public const int HistoryLength = 120;

        private static readonly float[] _frameHistory = new float[HistoryLength];

        private static int _historyCount;
        private static int _historyIndex;

        private static int _liveFixedSteps;
        private static int _liveCommands;
        private static int _liveDrawCalls;
        private static float _liveRenderMs;

        public static bool Enabled { get; set; }

        public static bool Visible { get; set; } = true;

        public static float FrameMs { get; private set; }
        public static float AverageFrameMs { get; private set; }
        public static float MinFrameMs { get; private set; }
        public static float MaxFrameMs { get; private set; }

        public static int FixedSteps { get; private set; }
        public static int RenderCommands { get; private set; }
        public static int DrawCalls { get; private set; }
        public static float RenderMs { get; private set; }

        public static float Fps => FrameMs > 0f ? 1000f / FrameMs : 0f;
        public static float AverageFps => AverageFrameMs > 0f ? 1000f / AverageFrameMs : 0f;

        public static float[] FrameHistory => _frameHistory;
        public static int FrameHistoryCount => _historyCount;
        public static int FrameHistoryOffset => _historyIndex;

        public static void Reset()
        {
            Array.Clear(_frameHistory);

            _historyCount = 0;
            _historyIndex = 0;

            FrameMs = 0f;
            AverageFrameMs = 0f;
            MinFrameMs = 0f;
            MaxFrameMs = 0f;
            FixedSteps = 0;
            RenderCommands = 0;
            DrawCalls = 0;
            RenderMs = 0f;
        }

        internal static void BeginFrame(float deltaTime)
        {
            if (!Enabled)
                return;

            FrameMs = deltaTime * 1000f;

            _frameHistory[_historyIndex] = FrameMs;
            _historyIndex = (_historyIndex + 1) % HistoryLength;

            if (_historyCount < HistoryLength)
                _historyCount++;

            float sum = 0f;
            float min = float.MaxValue;
            float max = 0f;

            for (int i = 0; i < _historyCount; i++)
            {
                float value = _frameHistory[i];
                sum += value;

                if (value < min)
                    min = value;

                if (value > max)
                    max = value;
            }

            AverageFrameMs = _historyCount > 0 ? sum / _historyCount : 0f;
            MinFrameMs = _historyCount > 0 ? min : 0f;
            MaxFrameMs = max;

            _liveFixedSteps = 0;
            _liveCommands = 0;
            _liveDrawCalls = 0;
            _liveRenderMs = 0f;
        }

        internal static void CountFixedStep()
        {
            _liveFixedSteps++;
        }

        internal static void RecordRender(int commands, int drawCalls, TimeSpan elapsed)
        {
            _liveCommands += commands;
            _liveDrawCalls += drawCalls;
            _liveRenderMs += (float)elapsed.TotalMilliseconds;
        }

        internal static void EndFrame()
        {
            if (!Enabled)
                return;

            FixedSteps = _liveFixedSteps;
            RenderCommands = _liveCommands;
            DrawCalls = _liveDrawCalls;
            RenderMs = _liveRenderMs;
        }
    }
}
