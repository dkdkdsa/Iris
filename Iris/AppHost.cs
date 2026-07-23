using Iris.Core;
using Iris.Platform;
using Iris.Rendering;
using System;
using System.Diagnostics;

namespace Iris
{
    public sealed class AppHost : IDisposable
    {
        private readonly IPlatform _platform;

        private Stopwatch _stopwatch;
        private double _previousTime;

        public IPlatform Platform => _platform;

        public ImGuiHost ImGui { get; private set; }

        public float DeltaTime { get; private set; }

        public bool IsCloseRequested => _platform.IsCloseRequested;

        public AppHost(IPlatform platform)
        {
            _platform = platform;
        }

        public void Initialize(WindowConfig config)
        {
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
        }

        public void Dispose()
        {
            ImGui?.Dispose();
            ImGui = null;

            _platform.Dispose();
        }
    }
}
