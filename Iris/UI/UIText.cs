using Iris.Assets;
using Iris.Core;
using Iris.Rendering;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace Iris.UI
{
    public class UIText : UIObject
    {
        public string Text { get; set; } = string.Empty;
        public IFont Font { get; set; }
        public float FontSize { get; set; } = 24f;
        public Color Color { get; set; } = new Color(255, 255, 255, 255);

        public override Vector2D<float> GetSize()
        {
            if (Font == null || string.IsNullOrEmpty(Text))
                return Vector2D<float>.Zero;

            float scale = FontSize / Font.BakePixelHeight;
            float maxWidth = 0f;
            float lineWidth = 0f;
            int lines = 1;

            foreach (char c in Text)
            {
                if (c == '\n')
                {
                    lines++;
                    maxWidth = MathF.Max(maxWidth, lineWidth);
                    lineWidth = 0f;
                    continue;
                }

                if (c == '\r')
                    continue;

                if (Font.TryGetGlyph(c, out var glyph))
                    lineWidth += glyph.Advance * scale;
            }

            maxWidth = MathF.Max(maxWidth, lineWidth);
            return new Vector2D<float>(maxWidth, lines * Font.LineHeight * scale);
        }

        public override void Render(Rectangle<float> screenRect, List<RenderCommand> output)
        {
            if (Font == null || string.IsNullOrEmpty(Text))
                return;

            var referenceSize = GetSize();

            if (referenceSize.X <= 0f || referenceSize.Y <= 0f)
                return;

            float uiScale = screenRect.Size.Y / referenceSize.Y;
            float scale = FontSize / Font.BakePixelHeight * uiScale;

            float penX = screenRect.Origin.X;
            float baseline = screenRect.Origin.Y + Font.Ascent * scale;

            foreach (char c in Text)
            {
                if (c == '\n')
                {
                    penX = screenRect.Origin.X;
                    baseline += Font.LineHeight * scale;
                    continue;
                }

                if (c == '\r')
                    continue;

                if (!Font.TryGetGlyph(c, out var glyph))
                    continue;

                if (glyph.Source.Size.X > 0 && glyph.Source.Size.Y > 0)
                {
                    output.Add(new RenderCommand
                    {
                        texture = Font.Atlas,
                        src = glyph.Source,
                        dest = new Rectangle<float>(
                            penX + glyph.Offset.X * scale,
                            baseline + glyph.Offset.Y * scale,
                            glyph.Source.Size.X * scale,
                            glyph.Source.Size.Y * scale),
                        order = Order,
                        color = Color,
                    });
                }

                penX += glyph.Advance * scale;
            }

            Font.Commit();
        }
    }
}
