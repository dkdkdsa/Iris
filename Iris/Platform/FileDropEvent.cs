using Silk.NET.Maths;

namespace Iris.Platform
{
    public readonly struct FileDropEvent
    {
        public string Path { get; }

        public Vector2D<int> Position { get; }

        public FileDropEvent(string path, Vector2D<int> position)
        {
            Path = path;
            Position = position;
        }
    }
}
