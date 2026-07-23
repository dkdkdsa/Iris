using Iris.Assets;
using Iris.Audio;
using Iris.Core;
using Iris.Physics;
using Iris.Platform;
using Iris.Rendering;
using Iris.UI.Events;
using Silk.NET.Maths;
using System;

namespace Iris
{
    public class Engine
    {
        private SystemManager _systemManager;
        private AppHost _host;
        private RenderSystem _renderSystem;

        private Vector2D<int> _backBufferSize;
        private double _fixedAccumulator;

        public event Action OnStart;

        public ImGuiHost ImGui => _host.ImGui;

        public Engine(IPlatform platform)
        {
            _systemManager = new SystemManager();
            _host = new AppHost(platform);
        }

        public bool Run(WindowConfig config)
        {
            Initialize(config);

            while (!_host.IsCloseRequested)
                Tick();

            Shutdown();
            return true;
        }

        public void Initialize(WindowConfig config)
        {
            _host.Initialize(config);

            AssetManager.Initialize();

            _backBufferSize = new Vector2D<int>(config.width, config.height);
            InitSystems(config);

            OnStart?.Invoke();
        }

        public void Tick()
        {
            if (!_host.BeginFrame())
                return;

            Time.DeltaTime = _host.DeltaTime;
            _fixedAccumulator += _host.DeltaTime;

            while (_fixedAccumulator >= Time.FixedTimeStep)
            {
                _systemManager.FixedUpdate();
                _fixedAccumulator -= Time.FixedTimeStep;
            }

            _systemManager.Update();
            _systemManager.LateUpdate();

            var window = _host.Platform.Window;
            _backBufferSize = new Vector2D<int>(window.Width, window.Height);

            _renderSystem.Viewport = _backBufferSize;
            _host.Present(_renderSystem.Flush);
        }

        public void Shutdown()
        {
            _systemManager.Dispose();
            _host.Dispose();
        }

        private void InitSystems(WindowConfig config)
        {
            var platform = _host.Platform;

            _renderSystem = new RenderSystem(platform.RenderBackend, config.width, config.height);
            var audioSystem = new AudioSystem(platform.AudioBackend);
            var actionSystem = new ActionScriptSystem();

            _systemManager.AddSystem(_renderSystem);
            _systemManager.AddSystem(audioSystem);
            _systemManager.AddSystem(actionSystem);
            _systemManager.CreateSystem<SceneSystem>();
            _systemManager.CreateSystem<PhysicsSystem>();
            _systemManager.CreateSystem<EventSystem>();
        }
    }
}
