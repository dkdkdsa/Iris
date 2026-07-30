using Iris.Core;
using Silk.NET.SDL;
using System.Collections.Generic;
using System.Text;
using KeyCode = Iris.Core.KeyCode;
using SdlKey = Silk.NET.SDL.KeyCode;

namespace Iris.Platform.SDL
{
    internal sealed class SDLInputBackend : InputBackendBase
    {
        private static readonly Dictionary<int, KeyCode> _keyMap = CreateKeyMap();

        public void ProcessEvent(in Event evt)
        {
            switch ((EventType)evt.Type)
            {
                case EventType.Keydown:
                    NotifyKeyDown(ToKeyCode(evt.Key.Keysym.Sym), evt.Key.Repeat != 0);
                    break;

                case EventType.Keyup:
                    NotifyKeyUp(ToKeyCode(evt.Key.Keysym.Sym));
                    break;

                case EventType.Mousebuttondown:
                    if (TryGetMouseButton(evt.Button.Button, out var pressed))
                        NotifyMouseButtonDown(pressed, evt.Button.X, evt.Button.Y);
                    break;

                case EventType.Mousebuttonup:
                    if (TryGetMouseButton(evt.Button.Button, out var released))
                        NotifyMouseButtonUp(released, evt.Button.X, evt.Button.Y);
                    break;

                case EventType.Mousemotion:
                    NotifyMouseMove(evt.Motion.X, evt.Motion.Y, evt.Motion.Xrel, evt.Motion.Yrel);
                    break;

                case EventType.Mousewheel:
                    NotifyMouseWheel(evt.Wheel.X, evt.Wheel.Y);
                    break;

                case EventType.Textinput:
                    NotifyText(ReadText(evt.Text));
                    break;
            }
        }

        private static unsafe string ReadText(TextInputEvent evt)
        {
            byte* text = evt.Text;

            int length = 0;
            while (length < 32 && text[length] != 0)
                length++;

            return length == 0 ? string.Empty : Encoding.UTF8.GetString(text, length);
        }

        private static KeyCode ToKeyCode(int sym)
        {
            return _keyMap.TryGetValue(sym, out var key) ? key : KeyCode.None;
        }

        private static bool TryGetMouseButton(byte button, out MouseButton result)
        {
            switch (button)
            {
                case Sdl.ButtonLeft: result = MouseButton.Left; return true;
                case Sdl.ButtonRight: result = MouseButton.Right; return true;
                case Sdl.ButtonMiddle: result = MouseButton.Middle; return true;
                case Sdl.ButtonX1: result = MouseButton.X1; return true;
                case Sdl.ButtonX2: result = MouseButton.X2; return true;
                default: result = default; return false;
            }
        }

        private static Dictionary<int, KeyCode> CreateKeyMap()
        {
            var map = new Dictionary<int, KeyCode>();

            for (int i = 0; i < 26; i++)
                map[(int)SdlKey.KA + i] = KeyCode.A + i;

            for (int i = 0; i < 10; i++)
                map[(int)SdlKey.K0 + i] = KeyCode.Alpha0 + i;

            for (int i = 0; i < 12; i++)
                map[(int)SdlKey.KF1 + i] = KeyCode.F1 + i;

            for (int i = 0; i < 9; i++)
                map[(int)SdlKey.KKP1 + i] = KeyCode.Keypad1 + i;
            map[(int)SdlKey.KKP0] = KeyCode.Keypad0;

            map[(int)SdlKey.KEscape] = KeyCode.Escape;
            map[(int)SdlKey.KReturn] = KeyCode.Return;
            map[(int)SdlKey.KSpace] = KeyCode.Space;
            map[(int)SdlKey.KTab] = KeyCode.Tab;
            map[(int)SdlKey.KBackspace] = KeyCode.Backspace;
            map[(int)SdlKey.KDelete] = KeyCode.Delete;
            map[(int)SdlKey.KInsert] = KeyCode.Insert;
            map[(int)SdlKey.KCapslock] = KeyCode.CapsLock;
            map[(int)SdlKey.KNumlockclear] = KeyCode.NumLock;
            map[(int)SdlKey.KScrolllock] = KeyCode.ScrollLock;
            map[(int)SdlKey.KPrintscreen] = KeyCode.PrintScreen;
            map[(int)SdlKey.KPause] = KeyCode.Pause;

            map[(int)SdlKey.KHome] = KeyCode.Home;
            map[(int)SdlKey.KEnd] = KeyCode.End;
            map[(int)SdlKey.KPageup] = KeyCode.PageUp;
            map[(int)SdlKey.KPagedown] = KeyCode.PageDown;
            map[(int)SdlKey.KUp] = KeyCode.UpArrow;
            map[(int)SdlKey.KDown] = KeyCode.DownArrow;
            map[(int)SdlKey.KLeft] = KeyCode.LeftArrow;
            map[(int)SdlKey.KRight] = KeyCode.RightArrow;

            map[(int)SdlKey.KLshift] = KeyCode.LeftShift;
            map[(int)SdlKey.KRshift] = KeyCode.RightShift;
            map[(int)SdlKey.KLctrl] = KeyCode.LeftControl;
            map[(int)SdlKey.KRctrl] = KeyCode.RightControl;
            map[(int)SdlKey.KLalt] = KeyCode.LeftAlt;
            map[(int)SdlKey.KRalt] = KeyCode.RightAlt;
            map[(int)SdlKey.KLgui] = KeyCode.LeftSuper;
            map[(int)SdlKey.KRgui] = KeyCode.RightSuper;

            map[(int)SdlKey.KMinus] = KeyCode.Minus;
            map[(int)SdlKey.KEquals] = KeyCode.Equals;
            map[(int)SdlKey.KLeftbracket] = KeyCode.LeftBracket;
            map[(int)SdlKey.KRightbracket] = KeyCode.RightBracket;
            map[(int)SdlKey.KBackslash] = KeyCode.Backslash;
            map[(int)SdlKey.KSemicolon] = KeyCode.Semicolon;
            map[(int)SdlKey.KQuote] = KeyCode.Quote;
            map[(int)SdlKey.KComma] = KeyCode.Comma;
            map[(int)SdlKey.KPeriod] = KeyCode.Period;
            map[(int)SdlKey.KSlash] = KeyCode.Slash;
            map[(int)SdlKey.KBackquote] = KeyCode.BackQuote;

            map[(int)SdlKey.KKPPeriod] = KeyCode.KeypadPeriod;
            map[(int)SdlKey.KKPDivide] = KeyCode.KeypadDivide;
            map[(int)SdlKey.KKPMultiply] = KeyCode.KeypadMultiply;
            map[(int)SdlKey.KKPMinus] = KeyCode.KeypadMinus;
            map[(int)SdlKey.KKPPlus] = KeyCode.KeypadPlus;
            map[(int)SdlKey.KKPEnter] = KeyCode.KeypadEnter;

            return map;
        }
    }
}
