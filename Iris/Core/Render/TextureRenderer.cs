using Iris.Assets;
using Iris.Rendering;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public sealed class TextureRenderer : RendererBase
    {
        public ITexture Texture { get; set; }


        protected override void Render()
        {
            if (Texture == null)
                return;

            var trm = OwnerActor.Transform;
            system.Submit(new RenderCommand
            {
                texture = Texture,
                dest = CreateDest(trm, Texture, new Vector2D<float>(0.5f, 0.5f)),
                rotation = trm.Rotation,
            });
        }


        private Rectangle<float> CreateDest(Transform transform, ITexture texture, Vector2D<float> pivot)
        {
            float width = texture.Width * transform.Scale.X;
            float height = texture.Height * transform.Scale.Y;
            float x = transform.Position.X - width * pivot.X;
            float y = transform.Position.Y - height * pivot.Y;
            return new Rectangle<float>(x, y, width, height);
        }
    }
}
