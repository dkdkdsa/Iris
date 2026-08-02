using System.Collections.Generic;

namespace Iris.Build.Atlas
{
    public sealed class ShelfPacker
    {
        private readonly List<Shelf> _shelves = new();
        private readonly int _width;
        private readonly int _height;
        private readonly int _padding;

        private int _used;

        public ShelfPacker(int width, int height, int padding)
        {
            _width = width;
            _height = height;
            _padding = padding;
        }

        public bool TryPlace(int width, int height, out int x, out int y)
        {
            x = 0;
            y = 0;

            int needWidth = width + _padding;
            int needHeight = height + _padding;

            if (needWidth > _width || needHeight > _height)
                return false;

            foreach (var shelf in _shelves)
            {
                if (needHeight > shelf.Height || shelf.Cursor + needWidth > _width)
                    continue;

                x = shelf.Cursor;
                y = shelf.Top;

                shelf.Cursor += needWidth;
                return true;
            }

            if (_used + needHeight > _height)
                return false;

            var created = new Shelf { Top = _used, Height = needHeight, Cursor = needWidth };

            _shelves.Add(created);
            _used += needHeight;

            x = 0;
            y = created.Top;

            return true;
        }

        private sealed class Shelf
        {
            public int Top;
            public int Height;
            public int Cursor;
        }
    }
}
