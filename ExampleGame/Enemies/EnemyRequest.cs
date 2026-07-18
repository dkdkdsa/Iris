using Iris.Core;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExampleGame.Enemies
{
    public class EnemyRequest
    {
        public Vector2D<float> targetPosition;
        public Transform target;
        public float speed;
    }
}
