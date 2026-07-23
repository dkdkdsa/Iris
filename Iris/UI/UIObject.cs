using Iris.Assets;
using Iris.Rendering;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace Iris.UI
{
    public class UIObject : IDisposable
    {
        public string Name { get; set; } = string.Empty;

        public Vector2D<float> Anchor { get; set; }

        public Vector2D<float> Position { get; set; }

        public Vector2D<float> Scale { get; set; } = Vector2D<float>.One;
        public float Rotation { get; set; } = 0f;
        public int Order { get; set; } = 0;
        public bool Visible { get; set; } = true;
        public ITexture Texture { get; set; }

        public virtual Vector2D<float> GetSize()
        {
            if (Texture == null)
                return Vector2D<float>.Zero;

            return new Vector2D<float>(Texture.Width * Scale.X, Texture.Height * Scale.Y);
        }

        public virtual void Render(Rectangle<float> screenRect, List<RenderCommand> output)
        {
            if (Texture == null)
                return;

            output.Add(new RenderCommand
            {
                texture = Texture,
                dest = screenRect,
                rotation = Rotation,
                order = Order,
            });
        }

        public virtual void Dispose()
        {
        }
    }
}
