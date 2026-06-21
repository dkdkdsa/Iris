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

        public int Width { get; init; }

        public int Height {  get; init; }

        public SDLWindow(Sdl sdl, WindowConfig config)
        {
            Width = config.width;
            Height = config.height;

            _window = sdl.CreateWindow(config.title, 
                Sdl.WindowposCentered, Sdl.WindowposCentered, 
                config.width, config.height, (uint)WindowFlags.Shown);

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
