using Box2D.NET;
using Iris.Attributes;
using Silk.NET.Maths;

namespace Iris.Core
{
    public class BoxCollider : Collider
    {
        private Vector2D<float> _size = Vector2D<float>.One;
        private Vector2D<float> _offset;

        [Show]
        public Vector2D<float> Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
                ApplyPolygon();
            }
        }

        [Show]
        public Vector2D<float> Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                _offset = value;
                ApplyPolygon();
            }
        }

        protected override B2ShapeId CreateShape(B2BodyId id)
        {
            var shapeDef = B2Types.b2DefaultShapeDef();
            shapeDef.density = 1f;
            shapeDef.enableSensorEvents = true;
            shapeDef.enableContactEvents = true;
            shapeDef.isSensor = IsSensor;
            shapeDef.material.friction = Friction;
            B2Polygon box = BuildPolygon();
            return B2Shapes.b2CreatePolygonShape(id, in shapeDef, in box);
        }

        private void ApplyPolygon()
        {
            if (!B2Worlds.b2Shape_IsValid(shapeId))
                return;

            B2Polygon box = BuildPolygon();
            B2Shapes.b2Shape_SetPolygon(shapeId, ref box);

            var body = rigid?.GetBodyId() ?? default;

            if (B2Worlds.b2Body_IsValid(body))
                B2Bodies.b2Body_ApplyMassFromShapes(body);
        }

        private B2Polygon BuildPolygon()
        {
            return B2Geometries.b2MakeOffsetBox(
                _size.X / 2f, _size.Y / 2f,
                new B2Vec2(_offset.X, _offset.Y),
                B2MathFunction.b2MakeRot(0f));
        }
    }
}
