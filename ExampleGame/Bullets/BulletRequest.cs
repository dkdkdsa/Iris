using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExampleGame.Bullets
{
    public struct BulletRequest
    {
        public float speed;
        public Vector2D<float> dir;
        public float lifetime;
        public string targetTag;
    }
}
