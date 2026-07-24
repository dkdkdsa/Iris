using Iris.Core;
using Silk.NET.Maths;

namespace Iris.Physics
{
    public struct CastHit
    {
        public Collider Collider;
        public Vector2D<float> Point;
        public Vector2D<float> Normal;
        public float Distance;
    }
}
