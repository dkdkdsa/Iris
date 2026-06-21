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
        private Sdl _sdl;
        private Event _evt;

        public SDLPlatform()
        {
            _sdl = Sdl.GetApi();
            _sdl.Init(Sdl.InitVideo);
            _backend = new SDLRenderBackend(_sdl);
        }

        bool IPlatform.IsCloseRequested => _closed;

        IWindow IPlatform.Window => _window;

        IRenderBackend IPlatform.Backend => _backend;


        void IPlatform.CreateWindow(WindowConfig config)
        {
            _window = new SDLWindow(_sdl, config);
            _backend.Init(_window);
        }

        void IPlatform.PumpEvents()
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
            _window?.Dispose();
            _backend?.Dispose();
            _sdl.Quit();
            _sdl.Dispose();
        }
    }
}
