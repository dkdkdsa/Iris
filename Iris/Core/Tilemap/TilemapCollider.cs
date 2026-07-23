using Box2D.NET;
using Iris.Physics;
using Silk.NET.Maths;
using System.Collections.Generic;

namespace Iris.Core
{
    public sealed class TilemapCollider : Collider
    {
        private readonly List<B2ShapeId> _shapes = new();
        private B2BodyId _bodyId;
        private bool _attached;
        private bool _built;

        protected override void OnAttached()
        {
            var system = SystemManager.Instance.GetSystem<PhysicsSystem>();
            var def = B2Types.b2DefaultBodyDef();
            def.type = B2BodyType.b2_staticBody;

            _bodyId = system.CreateBody(def);
            _attached = true;
        }

        public override void FixedUpdate()
        {
            if (_built)
                return;

            Rebuild();
        }

        public void Rebuild()
        {
            if (!_attached)
                return;

            _built = true;
            ClearShapes();

            var map = GetComponent<Tilemap>();

            if (map == null || map.Tiles.Count == 0)
                return;

            var grid = map.FindGrid();
            var origin = grid?.Origin ?? Transform.Position;
            var cellSize = grid?.CellSize ?? Vector2D<float>.One;

            if (cellSize.X <= 0f || cellSize.Y <= 0f)
                return;

            foreach (var (startX, y, length) in MergeRuns(map))
            {
                float width = length * cellSize.X;
                float height = cellSize.Y;
                float centerX = origin.X + startX * cellSize.X + width / 2f;
                float centerY = origin.Y + y * cellSize.Y + height / 2f;

                var shapeDef = B2Types.b2DefaultShapeDef();
                shapeDef.enableSensorEvents = true;
                shapeDef.enableContactEvents = true;
                shapeDef.isSensor = IsSensor;

                var box = B2Geometries.b2MakeOffsetBox(width / 2f, height / 2f,
                    new B2Vec2(centerX, centerY), B2MathFunction.b2MakeRot(0f));

                var shape = B2Shapes.b2CreatePolygonShape(_bodyId, in shapeDef, in box);
                B2Shapes.b2Shape_SetUserData(shape, new B2UserData(this));
                _shapes.Add(shape);
            }
        }

        protected override void ApplySensor()
        {
            if (_attached)
                Rebuild();
        }

        protected override B2ShapeId CreateShape(B2BodyId id)
        {
            return default;
        }

        public override void Dispose()
        {
            _shapes.Clear();
            _attached = false;

            if (B2Worlds.b2Body_IsValid(_bodyId))
                B2Bodies.b2DestroyBody(_bodyId);
        }

        private void ClearShapes()
        {
            foreach (var shape in _shapes)
            {
                if (B2Worlds.b2Shape_IsValid(shape))
                    B2Shapes.b2DestroyShape(shape, false);
            }

            _shapes.Clear();
        }

        private static IEnumerable<(int StartX, int Y, int Length)> MergeRuns(Tilemap map)
        {
            var cells = new HashSet<(int X, int Y)>();

            foreach (var cell in map.Tiles.Keys)
                cells.Add((cell.X, cell.Y));

            foreach (var (x, y) in cells)
            {
                if (cells.Contains((x - 1, y)))
                    continue;

                int length = 1;

                while (cells.Contains((x + length, y)))
                    length++;

                yield return (x, y, length);
            }
        }
    }
}
