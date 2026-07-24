using Iris.Rendering;
using Silk.NET.Maths;

namespace Iris.Core
{
    public sealed class SpriteRenderer : RendererBase
    {
        public Sprite Sprite { get; set; }
        public float PixelPerUnit { get; set; } = 100f;
        public Vector2D<float> Pivot { get; set; } = new Vector2D<float>(0.5f, 0.5f);
        public bool FlipX { get; set; }
        public bool FlipY { get; set; }
        public int Order { get; set; }
        public Color Color { get; set; } = new Color(255, 255, 255, 255);

        protected override void Render()
        {
            var texture = Sprite?.Texture;

            if (texture == null)
                return;

            var src = Sprite.SrcRect;
            float pixelWidth = src.HasValue ? src.Value.Size.X : texture.Width;
            float pixelHeight = src.HasValue ? src.Value.Size.Y : texture.Height;

            var trm = OwnerActor.Transform;
            float width = pixelWidth / PixelPerUnit * trm.Scale.X;
            float height = pixelHeight / PixelPerUnit * trm.Scale.Y;
            float x = trm.Position.X - width * Pivot.X;
            float y = trm.Position.Y - height * Pivot.Y;

            system.Submit(new RenderCommand
            {
                texture = texture,
                src = src,
                dest = new Rectangle<float>(x, y, width, height),
                rotation = trm.Rotation,
                flipX = FlipX,
                flipY = FlipY,
                order = Order,
                color = Color,
            });
        }
    }
}
