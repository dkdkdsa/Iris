using Iris.Audio;
using Iris.Audio.NAudio;
using Iris.Core;
using Iris.Platform.SDL;
using Iris.Rendering;
using Iris.Rendering.SDL;
using Silk.NET.SDL;
using System;

namespace Iris.Platform
{
    public class DefaultPlatform : IPlatform
    {
        private bool _closed = false;
        private SDLWindow _window;
        private SDLRenderBackend _renderBackend;
        private NAudioAudioBackend _audioBackend;
        private SDLInputBackend _inputBackend;
        private SDLClipboard _clipboard;
        private Sdl _sdl;
        private Event _evt;

        public DefaultPlatform()
        {
            _sdl = Sdl.GetApi();
            _sdl.Init(Sdl.InitAudio);
            _sdl.Init(Sdl.InitVideo);
            _renderBackend = new SDLRenderBackend(_sdl);
            _audioBackend = new NAudioAudioBackend();
            _inputBackend = new SDLInputBackend();
            _clipboard = new SDLClipboard(_sdl);

            _audioBackend.Init();

            Factories.Register(_renderBackend);
            Factories.Register(_audioBackend);
        }

        public event Action<FileDropEvent> FileDropped;

        public bool IsCloseRequested => _closed;

        public IWindow Window => _window;
        public IRenderBackend RenderBackend => _renderBackend;
        public IAudioBackend AudioBackend => _audioBackend;
        public IInputBackend InputBackend => _inputBackend;
        public IClipboard Clipboard => _clipboard;

        public void CreateWindow(WindowConfig config)
        {
            _window = new SDLWindow(_sdl, config);
            _renderBackend.VSync = config.vsync;
            _renderBackend.Init(_window);
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
            _renderBackend.Dispose();
            _sdl.Quit();
            _sdl.Dispose();
        }
    }
}
