using Hexa.NET.ImGui;
using Iris.Core;
using Iris.Platform;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Iris.Rendering
{
    public sealed unsafe class ImGuiHost : IDisposable
    {
        private static readonly KeyValuePair<KeyCode, ImGuiKey>[] _keyMap = CreateKeyMap();

        // UnmanagedCallersOnly는 캡처를 못 하므로 클립보드는 정적으로 잡아둔다.
        private static IClipboard _clipboard;
        private static nint _clipboardBuffer;

        private readonly IImGuiRenderer _renderer;
        private readonly IWindow _window;

        private ImGuiContextPtr _context;
        private bool _frameStarted;

        /// <summary>
        /// ImGui가 이번 프레임 입력을 가져갔는지. 게임 입력을 막을지는 호스트가 판단한다
        /// (에디터에서는 Scene 창 자체가 ImGui 창이라 이 값만으로 막으면 게임이 마우스를 영영 못 받는다).
        /// </summary>
        public bool WantCaptureMouse { get; private set; }
        public bool WantCaptureKeyboard { get; private set; }

        internal ImGuiHost(IImGuiRenderer renderer, IWindow window, IClipboard clipboard)
        {
            _renderer = renderer;
            _window = window;

            _context = ImGui.CreateContext();
            ImGui.SetCurrentContext(_context);

            var io = ImGui.GetIO();
            // RendererHasTextures가 있어야 1.92의 ImDrawData.Textures 경로가 채워진다.
            io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            SetupClipboard(clipboard);
        }

        /// <summary>AppHost가 프레임 초입에 부른다. 이후부터 ImGui 호출이 유효하다.</summary>
        internal void NewFrame(float deltaTime)
        {
            var io = ImGui.GetIO();

            io.DisplaySize = new Vector2(_window.Width, _window.Height);
            io.DeltaTime = deltaTime > 0f ? deltaTime : 1f / 60f;

            FeedInput(io);

            ImGui.NewFrame();
            _frameStarted = true;

            // NewFrame 이후에야 확정된다.
            WantCaptureMouse = io.WantCaptureMouse;
            WantCaptureKeyboard = io.WantCaptureKeyboard;
        }

        /// <summary>AppHost가 present 직전에 부른다.</summary>
        internal void Render()
        {
            if (!_frameStarted)
                return;

            _frameStarted = false;

            ImGui.Render();
            _renderer.RenderDrawData(ImGui.GetDrawData());
        }

        private static void FeedInput(ImGuiIOPtr io)
        {
            var mouse = Input.MousePosition;
            io.AddMousePosEvent(mouse.X, mouse.Y);

            io.AddMouseButtonEvent(0, Input.GetMouseButton(MouseButton.Left));
            io.AddMouseButtonEvent(1, Input.GetMouseButton(MouseButton.Right));
            io.AddMouseButtonEvent(2, Input.GetMouseButton(MouseButton.Middle));

            var scroll = Input.MouseScrollDelta;
            if (scroll.X != 0 || scroll.Y != 0)
                io.AddMouseWheelEvent(scroll.X, scroll.Y);

            for (int i = 0; i < _keyMap.Length; i++)
                io.AddKeyEvent(_keyMap[i].Value, Input.GetKey(_keyMap[i].Key));

            io.AddKeyEvent(ImGuiKey.ModCtrl, Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
            io.AddKeyEvent(ImGuiKey.ModShift, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            io.AddKeyEvent(ImGuiKey.ModAlt, Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
            io.AddKeyEvent(ImGuiKey.ModSuper, Input.GetKey(KeyCode.LeftSuper) || Input.GetKey(KeyCode.RightSuper));

            var text = Input.TextInput;
            for (int i = 0; i < text.Count; i++)
                io.AddInputCharacterUTF16(text[i]);
        }

        private static void SetupClipboard(IClipboard clipboard)
        {
            _clipboard = clipboard;

            if (clipboard == null)
                return;

            var platformIO = ImGui.GetPlatformIO();
            platformIO.PlatformGetClipboardTextFn = (void*)(delegate* unmanaged<ImGuiContext*, byte*>)&GetClipboardText;
            platformIO.PlatformSetClipboardTextFn = (void*)(delegate* unmanaged<ImGuiContext*, byte*, void>)&SetClipboardText;
        }

        // ImGui가 반환 포인터를 다음 호출까지 들고 있으므로 버퍼를 유지했다가 다음 번에 해제한다.
        [UnmanagedCallersOnly]
        private static byte* GetClipboardText(ImGuiContext* context)
        {
            try
            {
                FreeClipboardBuffer();

                _clipboardBuffer = Marshal.StringToCoTaskMemUTF8(_clipboard?.GetText() ?? string.Empty);
                return (byte*)_clipboardBuffer;
            }
            catch
            {
                return null; // 네이티브 콜백이라 예외가 넘어가면 프로세스가 죽는다.
            }
        }

        [UnmanagedCallersOnly]
        private static void SetClipboardText(ImGuiContext* context, byte* text)
        {
            try
            {
                _clipboard?.SetText(text == null ? string.Empty : Marshal.PtrToStringUTF8((nint)text) ?? string.Empty);
            }
            catch
            {
            }
        }

        private static void FreeClipboardBuffer()
        {
            if (_clipboardBuffer == 0)
                return;

            Marshal.FreeCoTaskMem(_clipboardBuffer);
            _clipboardBuffer = 0;
        }

        public void Dispose()
        {
            FreeClipboardBuffer();
            _clipboard = null;

            if (_context.IsNull)
                return;

            ImGui.DestroyContext(_context);
            _context = default;
        }

        private static KeyValuePair<KeyCode, ImGuiKey>[] CreateKeyMap()
        {
            var map = new List<KeyValuePair<KeyCode, ImGuiKey>>();

            void Add(KeyCode key, ImGuiKey imgui) => map.Add(new KeyValuePair<KeyCode, ImGuiKey>(key, imgui));

            for (int i = 0; i < 26; i++)
                Add(KeyCode.A + i, ImGuiKey.A + i);

            for (int i = 0; i < 10; i++)
                Add(KeyCode.Alpha0 + i, ImGuiKey.Key0 + i);

            for (int i = 0; i < 12; i++)
                Add(KeyCode.F1 + i, ImGuiKey.F1 + i);

            for (int i = 0; i < 10; i++)
                Add(KeyCode.Keypad0 + i, ImGuiKey.Keypad0 + i);

            Add(KeyCode.Escape, ImGuiKey.Escape);
            Add(KeyCode.Return, ImGuiKey.Enter);
            Add(KeyCode.Space, ImGuiKey.Space);
            Add(KeyCode.Tab, ImGuiKey.Tab);
            Add(KeyCode.Backspace, ImGuiKey.Backspace);
            Add(KeyCode.Delete, ImGuiKey.Delete);
            Add(KeyCode.Insert, ImGuiKey.Insert);
            Add(KeyCode.CapsLock, ImGuiKey.CapsLock);
            Add(KeyCode.NumLock, ImGuiKey.NumLock);
            Add(KeyCode.ScrollLock, ImGuiKey.ScrollLock);
            Add(KeyCode.PrintScreen, ImGuiKey.PrintScreen);
            Add(KeyCode.Pause, ImGuiKey.Pause);

            Add(KeyCode.Home, ImGuiKey.Home);
            Add(KeyCode.End, ImGuiKey.End);
            Add(KeyCode.PageUp, ImGuiKey.PageUp);
            Add(KeyCode.PageDown, ImGuiKey.PageDown);
            Add(KeyCode.UpArrow, ImGuiKey.UpArrow);
            Add(KeyCode.DownArrow, ImGuiKey.DownArrow);
            Add(KeyCode.LeftArrow, ImGuiKey.LeftArrow);
            Add(KeyCode.RightArrow, ImGuiKey.RightArrow);

            Add(KeyCode.LeftShift, ImGuiKey.LeftShift);
            Add(KeyCode.RightShift, ImGuiKey.RightShift);
            Add(KeyCode.LeftControl, ImGuiKey.LeftCtrl);
            Add(KeyCode.RightControl, ImGuiKey.RightCtrl);
            Add(KeyCode.LeftAlt, ImGuiKey.LeftAlt);
            Add(KeyCode.RightAlt, ImGuiKey.RightAlt);
            Add(KeyCode.LeftSuper, ImGuiKey.LeftSuper);
            Add(KeyCode.RightSuper, ImGuiKey.RightSuper);

            Add(KeyCode.Minus, ImGuiKey.Minus);
            Add(KeyCode.Equals, ImGuiKey.Equal);
            Add(KeyCode.LeftBracket, ImGuiKey.LeftBracket);
            Add(KeyCode.RightBracket, ImGuiKey.RightBracket);
            Add(KeyCode.Backslash, ImGuiKey.Backslash);
            Add(KeyCode.Semicolon, ImGuiKey.Semicolon);
            Add(KeyCode.Quote, ImGuiKey.Apostrophe);
            Add(KeyCode.Comma, ImGuiKey.Comma);
            Add(KeyCode.Period, ImGuiKey.Period);
            Add(KeyCode.Slash, ImGuiKey.Slash);
            Add(KeyCode.BackQuote, ImGuiKey.GraveAccent);

            Add(KeyCode.KeypadPeriod, ImGuiKey.KeypadDecimal);
            Add(KeyCode.KeypadDivide, ImGuiKey.KeypadDivide);
            Add(KeyCode.KeypadMultiply, ImGuiKey.KeypadMultiply);
            Add(KeyCode.KeypadMinus, ImGuiKey.KeypadSubtract);
            Add(KeyCode.KeypadPlus, ImGuiKey.KeypadAdd);
            Add(KeyCode.KeypadEnter, ImGuiKey.KeypadEnter);

            return map.ToArray();
        }
    }
}
