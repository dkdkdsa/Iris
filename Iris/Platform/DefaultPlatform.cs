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

            Factorys.Register(_renderBackend);
            Factorys.Register(_audioBackend);
        }

        public bool IsCloseRequested => _closed;

        public IWindow Window => _window;
        public IRenderBackend RenderBackend => _renderBackend;
        public IAudioBackend AudioBackend => _audioBackend;
        public IInputBackend InputBackend => _inputBackend;
        public IClipboard Clipboard => _clipboard;

        public void CreateWindow(WindowConfig config)
        {
            _window = new SDLWindow(_sdl, config);
            _renderBackend.Init(_window);
            _sdl.StartTextInput(); // 이게 있어야 SDL이 Textinput 이벤트를 보낸다.
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
            _renderBackend.Dispose();
            _sdl.Quit();
            _sdl.Dispose();
        }
    }
}
