using Silk.NET.SDL;

namespace Iris.Platform.SDL
{
    internal sealed class SDLClipboard : IClipboard
    {
        private readonly Sdl _sdl;

        public SDLClipboard(Sdl sdl)
        {
            _sdl = sdl;
        }

        public string GetText()
        {
            return _sdl.GetClipboardTextS() ?? string.Empty;
        }

        public void SetText(string text)
        {
            _sdl.SetClipboardText(text ?? string.Empty);
        }
    }
}
