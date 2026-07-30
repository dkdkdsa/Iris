using Iris.Core;
using Iris.Debugging;
using Iris.Platform;
using Iris.Rendering;
using System;
using System.Threading;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Iris
{
    public sealed class AppHost : IDisposable
    {
        private const double PreciseSleepThreshold = 0.002;
        private const double CoarseSleepThreshold = 0.020;
        private const double SpinMargin = 0.001;

        private readonly IPlatform _platform;

        private PrecisionTimer _frameTimer;
        private double _sleepThreshold;
        private Stopwatch _stopwatch;
        private double _previousTime;

        public IPlatform Platform => _platform;

        public ImGuiHost ImGui { get; private set; }

        public float DeltaTime { get; private set; }

        public int TargetFrameRate { get; set; }

        public bool IsCloseRequested => _platform.IsCloseRequested;

        public AppHost(IPlatform platform)
        {
            _platform = platform;
        }

        public void Initialize(WindowConfig config)
        {
            TargetFrameRate = config.targetFrameRate;

            _frameTimer = new PrecisionTimer();
            _sleepThreshold = _frameTimer.IsHighResolution ? PreciseSleepThreshold : CoarseSleepThreshold;

            if (TargetFrameRate > 0 && !_frameTimer.IsHighResolution)
                Debug.Channel("Iris").LogWarning("High-resolution timer is unavailable; frame limiting will be imprecise.");

            _platform.CreateWindow(config);
            Input.SetBackend(_platform.InputBackend);

            if (_platform.RenderBackend is IImGuiRenderer imguiRenderer)
                ImGui = new ImGuiHost(imguiRenderer, _platform.Window, _platform.Clipboard);

            _stopwatch = Stopwatch.StartNew();
            _previousTime = _stopwatch.Elapsed.TotalSeconds;
        }

        public bool BeginFrame()
        {
            double currentTime = _stopwatch.Elapsed.TotalSeconds;
            double frameTime = currentTime - _previousTime;
            _previousTime = currentTime;

            if (frameTime > 0.25)
                frameTime = 0.25;

            DeltaTime = (float)frameTime;

            Input.BeginFrame();
            _platform.PumpEvents();

            if (_platform.IsCloseRequested)
                return false;

            ImGui?.NewFrame(DeltaTime);
            return true;
        }

        public void Present(Action drawBackbuffer = null)
        {
            var backend = _platform.RenderBackend;

            backend.SetRenderTarget(null);
            backend.BeginFrame();
            backend.Clear(Camera.Main?.BackgroundColor ?? new Color(0, 0, 0, 255));

            drawBackbuffer?.Invoke();
            ImGui?.Render();

            backend.EndFrame();

            LimitFrameRate();
        }

        private void LimitFrameRate()
        {
            if (TargetFrameRate <= 0)
                return;

            double budget = 1.0 / TargetFrameRate;

            while (true)
            {
                double remaining = budget - (_stopwatch.Elapsed.TotalSeconds - _previousTime);

                if (remaining <= 0d)
                    return;

                if (remaining > _sleepThreshold)
                    _frameTimer.Wait(remaining - SpinMargin);
                else
                    Thread.SpinWait(64);
            }
        }

        public void Dispose()
        {
            ImGui?.Dispose();
            ImGui = null;

            _frameTimer?.Dispose();
            _frameTimer = null;

            _platform.Dispose();
        }
    }
}
