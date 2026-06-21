using Iris.Core;
using Iris.Platform;
using Iris.Rendering;
using System;
using System.Diagnostics;

namespace Iris
{
    public class Engine
    {
        private SystemManager _systemManager;
        private IPlatform _platform;
        public event Action OnStart;

        public Engine(IPlatform platform)
        {
            _systemManager = new SystemManager();
            _platform = platform;
        }

        public bool Run(WindowConfig config)
        {
            _platform.CreateWindow(config);

            InitSystems();

            bool running = true;

            var sw = Stopwatch.StartNew();
            double previousTime = sw.Elapsed.TotalSeconds;
            double fixedAccumulator = 0.0;

            OnStart?.Invoke();

            while (running)
            {
                double currentTime = sw.Elapsed.TotalSeconds;
                double frameTime = currentTime - previousTime;
                previousTime = currentTime;

                if (frameTime > 0.25)
                    frameTime = 0.25;

                Time.DeltaTime = (float)frameTime;
                fixedAccumulator += frameTime;

                Input.BeginFrame();
                _platform.PumpEvents();

                running = !_platform.IsCloseRequested;
                if (!running)
                    break;

                while (fixedAccumulator >= Time.FixedTimeStep)
                {
                    FixedUpdate();
                    fixedAccumulator -= Time.FixedTimeStep;
                }

                Update();
            }

            _systemManager.Dispose();
            _platform.Dispose();

            return true;
        }

        private void InitSystems()
        {

            var renderSystem = new RenderSystem(_platform.Backend);
            var actionSystem = new ActionScriptSystem();

            _systemManager.AddSystem(renderSystem);
            _systemManager.AddSystem(actionSystem);
        }

        private void Update()
        {
            _systemManager.Update();
        }

        private void FixedUpdate()
        {
            _systemManager.FixedUpdate();
        }
    }
}