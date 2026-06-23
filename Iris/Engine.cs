using Iris.Assets;
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
        public TextureManager TextureMgr { get; internal set; }
        public event Action OnStart;

        public Engine(IPlatform platform)
        {
            _systemManager = new SystemManager();
            _platform = platform;
        }

        public bool Run(WindowConfig config)
        {
            _platform.CreateWindow(config);

            InitSystems(config);
            new World();
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
                LateUpdate();
            }

            _systemManager.Dispose();
            _platform.Dispose();

            return true;
        }

        private void InitSystems(WindowConfig config)
        {
            var renderSystem = new RenderSystem(_platform.Backend, config.width, config.height);
            TextureMgr = new TextureManager((ITextureFactory)_platform.Backend, new StbImageDecoder());
            var actionSystem = new ActionScriptSystem();

            _systemManager.AddSystem(renderSystem);
            _systemManager.AddSystem(actionSystem);
        }

        private void Update()
        {
            World.CurrentWorld.Update();
            _systemManager.Update();
        }

        private void LateUpdate()
        {
            World.CurrentWorld.LateUpdate();
            _systemManager.LateUpdate();
        }

        private void FixedUpdate()
        {
            World.CurrentWorld.FixedUpdate();
            _systemManager.FixedUpdate();
        }
    }
}