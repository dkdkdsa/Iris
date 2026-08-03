using Iris.Core;
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

        public float Width { get; set; }
        public float Height { get; set; }

        public Vector2D<float> Scale { get; set; } = Vector2D<float>.One;
        public float Rotation { get; set; } = 0f;
        public int Order { get; set; } = 0;
        public bool Visible { get; set; } = true;
        public Sprite Sprite { get; set; }

        protected virtual Vector2D<float> GetNativeSize()
        {
            var texture = Sprite?.Texture;

            if (texture == null)
                return Vector2D<float>.Zero;

            var src = Sprite.SrcRect;

            return new Vector2D<float>(
                src.HasValue ? src.Value.Size.X : texture.Width,
                src.HasValue ? src.Value.Size.Y : texture.Height);
        }

        public virtual Vector2D<float> GetSize()
        {
            var native = GetNativeSize();

            float width = Width > 0f ? Width : native.X;
            float height = Height > 0f ? Height : native.Y;

            return new Vector2D<float>(width * Scale.X, height * Scale.Y);
        }

        public virtual void Render(Rectangle<float> screenRect, List<RenderCommand> output)
        {
            var texture = Sprite?.Texture;

            if (texture == null)
                return;

            output.Add(new RenderCommand
            {
                texture = texture,
                src = Sprite.SrcRect,
                dest = screenRect,
                rotation = Rotation,
                flipX = Scale.X < 0f,
                flipY = Scale.Y < 0f,
                order = Order,
            });
        }

        public virtual void Dispose()
        {
        }
    }
}
