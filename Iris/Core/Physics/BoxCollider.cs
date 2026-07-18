using Box2D.NET;
using Silk.NET.Maths;

namespace Iris.Core
{
    public class BoxCollider : Collider
    {
        private Vector2D<float> _size = Vector2D<float>.One;

        public Vector2D<float> Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
                B2Polygon box = B2Geometries.b2MakeBox(value.X / 2, value.Y / 2);
                B2Shapes.b2Shape_SetPolygon(shapeId, ref box);
            }
        }


        protected override B2ShapeId CreateShape(B2BodyId id)
        {
            var shapeDef = B2Types.b2DefaultShapeDef();
            shapeDef.density = 1f;
            shapeDef.enableSensorEvents = true;
            shapeDef.enableContactEvents = true;
            shapeDef.isSensor = IsSensor;
            B2Polygon box = B2Geometries.b2MakeBox(_size.X / 2, _size.Y / 2);
            return B2Shapes.b2CreatePolygonShape(id, in shapeDef, in box);
        }
    }
}
