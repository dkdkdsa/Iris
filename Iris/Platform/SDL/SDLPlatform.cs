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

            Factories.Register(_backend);
        }

        public event Action<FileDropEvent> FileDropped;

        public bool IsCloseRequested => _closed;

        public IWindow Window => _window;

        public IRenderBackend RenderBackend => _backend;

        public IAudioBackend AudioBackend => throw new NotImplementedException();

        public IInputBackend InputBackend => _inputBackend;

        public IClipboard Clipboard => _clipboard;

        public void CreateWindow(WindowConfig config)
        {
            _window = new SDLWindow(_sdl, config);
            _backend.VSync = config.vsync;
            _backend.Init(_window);
            _sdl.StartTextInput();
            _sdl.EventState((uint)EventType.Dropfile, Sdl.Enable);
        }

        public void PumpEvents()
        {
            while (_sdl.PollEvent(ref _evt) != 0)
            {
                if (_evt.Type == (uint)EventType.Quit)
                {
                    _closed = true;
                }

                if (_evt.Type == (uint)EventType.Dropfile)
                    RaiseFileDropped(_evt);

                _inputBackend.ProcessEvent(_evt);
            }
        }

        private unsafe void RaiseFileDropped(in Event evt)
        {
            byte* file = evt.Drop.File;

            if (file == null)
                return;

            try
            {
                string path = System.Runtime.InteropServices.Marshal.PtrToStringUTF8((nint)file);

                if (!string.IsNullOrEmpty(path))
                    FileDropped?.Invoke(new FileDropEvent(path, GetDropPosition()));
            }
            finally
            {
                _sdl.Free(file);
            }
        }

        private unsafe Silk.NET.Maths.Vector2D<int> GetDropPosition()
        {
            if (_window == null)
                return default;

            int globalX, globalY, windowX, windowY;

            _sdl.GetGlobalMouseState(&globalX, &globalY);
            _sdl.GetWindowPosition(_window.GetNativeWindow(), &windowX, &windowY);

            return new Silk.NET.Maths.Vector2D<int>(globalX - windowX, globalY - windowY);
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
