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

        // 리사이즈/풀스크린이면 크기가 수시로 변하므로 항상 실제 창 크기를 조회한다.
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

            // 해상도를 바꾸는 독점 풀스크린 대신 데스크톱 해상도 무경계 창 방식.
            // 알트탭·멀티모니터에서 안전하고 요즘 게임들의 기본값이다.
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
