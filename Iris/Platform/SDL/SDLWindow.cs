using Iris.Core;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Platform.SDL
{
    internal unsafe class SDLWindow : IWindow
    {
        private Window* _window;
        private Sdl _sdl;

        public int Width
        {
            get
            {
                int w, h;
                _sdl.GetWindowSize(_window, &w, &h);
                return w;
            }
        }

        public int Height
        {
            get
            {
                int w, h;
                _sdl.GetWindowSize(_window, &w, &h);
                return h;
            }
        }

        public SDLWindow(Sdl sdl, WindowConfig config)
        {
            var flags = WindowFlags.Shown;

            if (config.resizable)
                flags |= WindowFlags.Resizable;

            if (config.fullscreen)
                flags |= WindowFlags.FullscreenDesktop;

            _window = sdl.CreateWindow(config.title,
                Sdl.WindowposCentered, Sdl.WindowposCentered,
                config.width, config.height, (uint)flags);

           _sdl = sdl;
        }

        public Window* GetNativeWindow()
        {
            return _window;
        }

        public void Dispose()
        {
            _sdl.DestroyWindow(_window);
        }
    }
}
