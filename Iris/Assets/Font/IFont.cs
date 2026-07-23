namespace Iris.Assets
{
    public interface IFont : IAsset
    {
        public ITexture Atlas { get; }
        public float BakePixelHeight { get; }
        public float Ascent { get; }
        public float LineHeight { get; }
        public bool TryGetGlyph(int codepoint, out FontGlyph glyph);
        public void Commit();
    }
}
