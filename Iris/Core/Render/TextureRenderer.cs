using Iris.Assets;
using Iris.Rendering;
using Silk.NET.Maths;

namespace Iris.Core
{
    public sealed class TextureRenderer : RendererBase
    {
        public ITexture Texture { get; set; }
        public float PixelPerUnit { get; set; } = 100f;
        public Vector2D<float> Pivot { get; set; } = new Vector2D<float>(0.5f, 0.5f);
        public bool FlipX { get; set; }
        public bool FlipY { get; set; }
        public int Order { get; set; }
        public Color Color { get; set; } = new Color(255, 255, 255, 255);

        protected override void Render()
        {
            if (Texture == null)
                return;

            var trm = OwnerActor.Transform;
            system.Submit(new RenderCommand
            {
                texture = Texture,
                dest = CreateDest(trm, Texture, Pivot),
                rotation = trm.Rotation,
                flipX = FlipX,
                flipY = FlipY,
                order = Order,
                color = Color,
            });
        }

        private Rectangle<float> CreateDest(Transform transform, ITexture texture, Vector2D<float> pivot)
        {
            float width = texture.Width / PixelPerUnit * transform.Scale.X;
            float height = texture.Height / PixelPerUnit * transform.Scale.Y;
            float x = transform.Position.X - width * pivot.X;
            float y = transform.Position.Y - height * pivot.Y;
            return new Rectangle<float>(x, y, width, height);
        }
    }
}
