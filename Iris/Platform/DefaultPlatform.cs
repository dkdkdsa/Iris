using Iris.Audio;
using Iris.Audio.NAudio;
using Iris.Core;
using Iris.Platform.SDL;
using Iris.Rendering;
using Iris.Rendering.SDL;
using Silk.NET.SDL;

namespace Iris.Platform
{
    public class DefaultPlatform : IPlatform
    {
        private bool _closed = false;
        private SDLWindow _window;
        private SDLRenderBackend _renderBackend;
        private NAudioAudioBackend _audioBackend;
        private Sdl _sdl;
        private Event _evt;

        public DefaultPlatform()
        {
            _sdl = Sdl.GetApi();
            _sdl.Init(Sdl.InitAudio);
            _sdl.Init(Sdl.InitVideo);
            _renderBackend = new SDLRenderBackend(_sdl);
            _audioBackend = new NAudioAudioBackend();

            _audioBackend.Init();

            Factorys.Register(_renderBackend);
            Factorys.Register(_audioBackend);
        }

        public bool IsCloseRequested => _closed;

        public IWindow Window => _window;
        public IRenderBackend RenderBackend => _renderBackend;
        public IAudioBackend AudioBackend => _audioBackend;

        public void CreateWindow(WindowConfig config)
        {
            _window = new SDLWindow(_sdl, config);
            _renderBackend.Init(_window);
        }

        public void PumpEvents()
        {
            while (_sdl.PollEvent(ref _evt) != 0)
            {
                if (_evt.Type == (uint)EventType.Quit)
                {
                    _closed = true;
                }

                if (_evt.Type == (uint)EventType.Keydown)
                {
                    Input.NotifyKeyDown((KeyCode)_evt.Key.Keysym.Sym, _evt.Key.Repeat != 0);
                }

                if (_evt.Type == (uint)EventType.Keyup)
                {
                    Input.NotifyKeyUp((KeyCode)_evt.Key.Keysym.Sym);
                }

                if (_evt.Type == (uint)EventType.Mousebuttondown)
                {
                    Input.NotifyMouseButtonDown(_evt.Button.Button, _evt.Button.X, _evt.Button.Y);
                }

                if (_evt.Type == (uint)EventType.Mousebuttonup)
                {
                    Input.NotifyMouseButtonUp(_evt.Button.Button, _evt.Button.X, _evt.Button.Y);
                }

                if (_evt.Type == (uint)EventType.Mousemotion)
                {
                    Input.NotifyMouseMove(_evt.Motion.X, _evt.Motion.Y, _evt.Motion.Xrel, _evt.Motion.Yrel);
                }
            }
        }

        public void Dispose()
        {
            _window.Dispose();
            _renderBackend.Dispose();
            _sdl.Quit();
            _sdl.Dispose();
        }
    }
}
