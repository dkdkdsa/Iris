using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public sealed class Transform : Component
    {
        public Vector2D<float> Position { get; set; }
        public float Rotation { get; set; }
        public Vector2D<float> Scale { get; set; } = Vector2D<float>.One;
    }
}
