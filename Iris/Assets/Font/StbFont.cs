using Iris.Core;
using Iris.Debugging;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static StbTrueTypeSharp.StbTrueType;

namespace Iris.Assets
{
    internal unsafe class StbFont : IFont
    {
        private const int AtlasWidth = 2048;
        private const int AtlasHeight = 2048;
        private const int Padding = 1;

        private readonly stbtt_fontinfo _info;
        private GCHandle _dataPin;
        private readonly float _scale;

        private readonly Dictionary<int, FontGlyph> _glyphs = new();
        private readonly byte[] _pixels = new byte[AtlasWidth * AtlasHeight];
        private Color[] _colors;

        private int _penX = Padding;
        private int _penY = Padding;
        private int _rowHeight;
        private bool _dirty;
        private bool _warnedFull;

        public ITexture Atlas { get; }

        public float BakePixelHeight { get; }

        public float Ascent { get; }

        public float LineHeight { get; }

        public StbFont(byte[] data, float bakePixelHeight = 32f)
        {
            _dataPin = GCHandle.Alloc(data, GCHandleType.Pinned);
            byte* ptr = (byte*)_dataPin.AddrOfPinnedObject();

            _info = new stbtt_fontinfo();

            if (stbtt_InitFont(_info, ptr, stbtt_GetFontOffsetForIndex(ptr, 0)) == 0)
            {
                _dataPin.Free();
                throw new InvalidOperationException("Failed to parse font file.");
            }

            BakePixelHeight = bakePixelHeight;
            _scale = stbtt_ScaleForPixelHeight(_info, bakePixelHeight);

            int ascent, descent, lineGap;
            stbtt_GetFontVMetrics(_info, &ascent, &descent, &lineGap);

            Ascent = ascent * _scale;
            LineHeight = (ascent - descent + lineGap) * _scale;

            Atlas = Factorys.Create<ITexture, Vector2D<int>>(new(AtlasWidth, AtlasHeight));
        }

        public bool TryGetGlyph(int codepoint, out FontGlyph glyph)
        {
            if (_glyphs.TryGetValue(codepoint, out glyph))
                return true;

            glyph = Bake(codepoint);
            _glyphs[codepoint] = glyph;
            return true;
        }

        public void Commit()
        {
            if (!_dirty || Atlas == null)
                return;

            _colors ??= new Color[AtlasWidth * AtlasHeight];

            for (int i = 0; i < _pixels.Length; i++)
                _colors[i] = new Color(255, 255, 255, _pixels[i]);

            Atlas.UpdateTexture(_colors);
            _dirty = false;
        }

        private FontGlyph Bake(int codepoint)
        {
            int glyphIndex = stbtt_FindGlyphIndex(_info, codepoint);

            int advance, bearing;
            stbtt_GetGlyphHMetrics(_info, glyphIndex, &advance, &bearing);

            var glyph = new FontGlyph { Advance = advance * _scale };

            if (glyphIndex == 0 && codepoint != ' ')
                return glyph;

            int x0, y0, x1, y1;
            stbtt_GetGlyphBitmapBox(_info, glyphIndex, _scale, _scale, &x0, &y0, &x1, &y1);

            int width = x1 - x0;
            int height = y1 - y0;

            if (width <= 0 || height <= 0)
                return glyph;

            if (_penX + width + Padding > AtlasWidth)
            {
                _penX = Padding;
                _penY += _rowHeight + Padding;
                _rowHeight = 0;
            }

            if (_penY + height + Padding > AtlasHeight)
            {
                if (!_warnedFull)
                {
                    _warnedFull = true;
                    Debug.LogWarning("Font atlas is full; further glyphs will not be rendered.");
                }

                return glyph;
            }

            fixed (byte* pixels = _pixels)
            {
                stbtt_MakeGlyphBitmap(_info,
                    pixels + _penY * AtlasWidth + _penX,
                    width, height, AtlasWidth, _scale, _scale, glyphIndex);
            }

            glyph.Source = new Rectangle<int>(_penX, _penY, width, height);
            glyph.Offset = new Vector2D<float>(x0, y0);

            _penX += width + Padding;
            _rowHeight = Math.Max(_rowHeight, height);
            _dirty = true;

            return glyph;
        }

        public void Dispose()
        {
            Atlas?.Dispose();

            if (_dataPin.IsAllocated)
                _dataPin.Free();
        }
    }
}
