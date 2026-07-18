namespace Iris.Core
{
    public enum KeyCode
    {
        None = 0,

        // 문자
        A = 1, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

        // 숫자 (상단 행)
        Alpha0 = 30, Alpha1, Alpha2, Alpha3, Alpha4,
        Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,

        // 펑션
        F1 = 50, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

        // 편집 / 제어
        Escape = 70, Return, Space, Tab, Backspace, Delete, Insert,
        CapsLock, NumLock, ScrollLock, PrintScreen, Pause,

        // 이동
        Home = 90, End, PageUp, PageDown,
        UpArrow, DownArrow, LeftArrow, RightArrow,

        // 조합
        LeftShift = 110, RightShift, LeftControl, RightControl,
        LeftAlt, RightAlt, LeftSuper, RightSuper,

        // 기호
        Minus = 130, Equals, LeftBracket, RightBracket, Backslash,
        Semicolon, Quote, Comma, Period, Slash, BackQuote,

        // 키패드
        Keypad0 = 150, Keypad1, Keypad2, Keypad3, Keypad4,
        Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
        KeypadPeriod, KeypadDivide, KeypadMultiply, KeypadMinus,
        KeypadPlus, KeypadEnter,
    }
}
