namespace Iris.Platform
{
    public interface IClipboard
    {
        public string GetText();
        public void SetText(string text);
    }
}
