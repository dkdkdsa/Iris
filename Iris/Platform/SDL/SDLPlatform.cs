using Iris.Audio;
using Iris.Core;
using Iris.Rendering;
using Iris.Rendering.SDL;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Platform.SDL
{
    public class SDLPlatform : IPlatform
    {
        private bool _closed = false;
        private SDLWindow _window;
        private SDLRenderBackend _backend;
        private SDLInputBackend _inputBackend;
        private SDLClipboard _clipboard;
        private Sdl _sdl;
        private Event _evt;

        public SDLPlatform()
        {
            _sdl = Sdl.GetApi();
            _sdl.Init(Sdl.InitAudio);
            _sdl.Init(Sdl.InitVideo);
            _backend = new SDLRenderBackend(_sdl);
            _inputBackend = new SDLInputBackend();
            _clipboard = new SDLClipboard(_sdl);

            Factorys.Register(_backend);
        }

        public bool IsCloseRequested => _closed;

        public IWindow Window => _window;

        public IRenderBackend RenderBackend => _backend;

        public IAudioBackend AudioBackend => throw new NotImplementedException();

        public IInputBackend InputBackend => _inputBackend;

        public IClipboard Clipboard => _clipboard;

        public void CreateWindow(WindowConfig config)
        {
            _window = new SDLWindow(_sdl, config);
            _backend.Init(_window);
            _sdl.StartTextInput();
        }

        public void PumpEvents()
        {
            while (_sdl.PollEvent(ref _evt) != 0)
            {
                if (_evt.Type == (uint)EventType.Quit)
                {
                    _closed = true;
                }

                _inputBackend.ProcessEvent(_evt);
            }
        }

        public void Dispose()
        {
            _window.Dispose();
            _backend.Dispose();
            _sdl.Quit();
            _sdl.Dispose();
        }
    }
}
