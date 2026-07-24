using Box2D.NET;
using Iris.Core;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace Iris.Physics
{
    public static class Casting
    {
        private sealed class ClosestState
        {
            public bool Hit;
            public float Fraction;
            public B2ShapeId Shape;
            public B2Vec2 Point;
            public B2Vec2 Normal;
        }

        private sealed class AllState
        {
            public List<CastHit> Results;
            public float Distance;
        }

        private sealed class OverlapFirstState
        {
            public bool Hit;
            public B2ShapeId Shape;
        }

        private sealed class OverlapAllState
        {
            public List<Collider> Results;
        }

        private static readonly b2CastResultFcn _closestFcn = ClosestCallback;
        private static readonly b2CastResultFcn _allFcn = AllCallback;
        private static readonly b2OverlapResultFcn _overlapFirstFcn = OverlapFirstCallback;
        private static readonly b2OverlapResultFcn _overlapAllFcn = OverlapAllCallback;

        private static readonly ClosestState _closest = new();
        private static readonly AllState _all = new();
        private static readonly OverlapFirstState _overlapFirst = new();
        private static readonly OverlapAllState _overlapAll = new();

        public static bool Raycast(Vector2D<float> origin, Vector2D<float> direction, float distance, out CastHit hit)
        {
            hit = default;

            if (!TryBegin(direction, distance, out var world, out var translation))
                return false;

            var filter = B2Types.b2DefaultQueryFilter();
            var state = ResetClosest();

            B2Worlds.b2World_CastRay(world, ToB2(origin), translation, in filter, _closestFcn, state);

            return TryBuildHit(state, distance, out hit);
        }

        public static int RaycastAll(Vector2D<float> origin, Vector2D<float> direction, float distance, List<CastHit> results)
        {
            if (results == null)
                return 0;

            results.Clear();

            if (!TryBegin(direction, distance, out var world, out var translation))
                return 0;

            var filter = B2Types.b2DefaultQueryFilter();
            var state = ResetAll(results, distance);

            B2Worlds.b2World_CastRay(world, ToB2(origin), translation, in filter, _allFcn, state);

            results.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));
            return results.Count;
        }

        public static bool BoxCast(Vector2D<float> origin, Vector2D<float> size, Vector2D<float> direction, float distance,
            out CastHit hit, float angle = 0f)
        {
            hit = default;

            if (!TryBegin(direction, distance, out var world, out var translation))
                return false;

            if (size.X <= 0f || size.Y <= 0f)
                return false;

            var proxy = BoxProxy(origin, size, angle);
            var filter = B2Types.b2DefaultQueryFilter();
            var state = ResetClosest();

            B2Worlds.b2World_CastShape(world, ref proxy, translation, in filter, _closestFcn, state);

            return TryBuildHit(state, distance, out hit);
        }

        public static int BoxCastAll(Vector2D<float> origin, Vector2D<float> size, Vector2D<float> direction, float distance,
            List<CastHit> results, float angle = 0f)
        {
            if (results == null)
                return 0;

            results.Clear();

            if (!TryBegin(direction, distance, out var world, out var translation))
                return 0;

            if (size.X <= 0f || size.Y <= 0f)
                return 0;

            var proxy = BoxProxy(origin, size, angle);
            var filter = B2Types.b2DefaultQueryFilter();
            var state = ResetAll(results, distance);

            B2Worlds.b2World_CastShape(world, ref proxy, translation, in filter, _allFcn, state);

            results.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));
            return results.Count;
        }

        public static bool CircleCast(Vector2D<float> origin, float radius, Vector2D<float> direction, float distance, out CastHit hit)
        {
            hit = default;

            if (!TryBegin(direction, distance, out var world, out var translation))
                return false;

            if (radius <= 0f)
                return false;

            var proxy = B2Distances.b2MakeProxy(ToB2(origin), 1, radius);
            var filter = B2Types.b2DefaultQueryFilter();
            var state = ResetClosest();

            B2Worlds.b2World_CastShape(world, ref proxy, translation, in filter, _closestFcn, state);

            return TryBuildHit(state, distance, out hit);
        }

        public static int CircleCastAll(Vector2D<float> origin, float radius, Vector2D<float> direction, float distance, List<CastHit> results)
        {
            if (results == null)
                return 0;

            results.Clear();

            if (!TryBegin(direction, distance, out var world, out var translation))
                return 0;

            if (radius <= 0f)
                return 0;

            var proxy = B2Distances.b2MakeProxy(ToB2(origin), 1, radius);
            var filter = B2Types.b2DefaultQueryFilter();
            var state = ResetAll(results, distance);

            B2Worlds.b2World_CastShape(world, ref proxy, translation, in filter, _allFcn, state);

            results.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));
            return results.Count;
        }

        public static bool OverlapBox(Vector2D<float> center, Vector2D<float> size, out Collider collider, float angle = 0f)
        {
            collider = null;

            if (!TryGetWorld(out var world) || size.X <= 0f || size.Y <= 0f)
                return false;

            var proxy = BoxProxy(center, size, angle);
            return OverlapFirst(world, ref proxy, out collider);
        }

        public static int OverlapBoxAll(Vector2D<float> center, Vector2D<float> size, List<Collider> results, float angle = 0f)
        {
            if (results == null)
                return 0;

            results.Clear();

            if (!TryGetWorld(out var world) || size.X <= 0f || size.Y <= 0f)
                return 0;

            var proxy = BoxProxy(center, size, angle);
            return OverlapAll(world, ref proxy, results);
        }

        public static bool OverlapCircle(Vector2D<float> center, float radius, out Collider collider)
        {
            collider = null;

            if (!TryGetWorld(out var world) || radius <= 0f)
                return false;

            var proxy = B2Distances.b2MakeProxy(ToB2(center), 1, radius);
            return OverlapFirst(world, ref proxy, out collider);
        }

        public static int OverlapCircleAll(Vector2D<float> center, float radius, List<Collider> results)
        {
            if (results == null)
                return 0;

            results.Clear();

            if (!TryGetWorld(out var world) || radius <= 0f)
                return 0;

            var proxy = B2Distances.b2MakeProxy(ToB2(center), 1, radius);
            return OverlapAll(world, ref proxy, results);
        }

        public static bool OverlapPoint(Vector2D<float> point, out Collider collider)
        {
            collider = null;

            if (!TryGetWorld(out var world))
                return false;

            var proxy = B2Distances.b2MakeProxy(ToB2(point), 1, 0f);
            return OverlapFirst(world, ref proxy, out collider);
        }

        public static int OverlapPointAll(Vector2D<float> point, List<Collider> results)
        {
            if (results == null)
                return 0;

            results.Clear();

            if (!TryGetWorld(out var world))
                return 0;

            var proxy = B2Distances.b2MakeProxy(ToB2(point), 1, 0f);
            return OverlapAll(world, ref proxy, results);
        }

        private static bool OverlapFirst(B2WorldId world, ref B2ShapeProxy proxy, out Collider collider)
        {
            var filter = B2Types.b2DefaultQueryFilter();
            var state = ResetOverlapFirst();

            B2Worlds.b2World_OverlapShape(world, ref proxy, in filter, _overlapFirstFcn, state);

            collider = state.Hit ? ColliderOf(state.Shape) : null;
            return state.Hit;
        }

        private static int OverlapAll(B2WorldId world, ref B2ShapeProxy proxy, List<Collider> results)
        {
            var filter = B2Types.b2DefaultQueryFilter();
            var state = ResetOverlapAll(results);

            B2Worlds.b2World_OverlapShape(world, ref proxy, in filter, _overlapAllFcn, state);

            return results.Count;
        }

        private static bool OverlapFirstCallback(B2ShapeId shapeId, object context)
        {
            if (B2Shapes.b2Shape_IsSensor(shapeId))
                return true;

            var state = (OverlapFirstState)context;

            state.Hit = true;
            state.Shape = shapeId;

            return false;
        }

        private static bool OverlapAllCallback(B2ShapeId shapeId, object context)
        {
            if (B2Shapes.b2Shape_IsSensor(shapeId))
                return true;

            var state = (OverlapAllState)context;
            state.Results.Add(ColliderOf(shapeId));

            return true;
        }

        private static OverlapFirstState ResetOverlapFirst()
        {
            _overlapFirst.Hit = false;
            _overlapFirst.Shape = default;
            return _overlapFirst;
        }

        private static OverlapAllState ResetOverlapAll(List<Collider> results)
        {
            _overlapAll.Results = results;
            return _overlapAll;
        }

        private static float ClosestCallback(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context)
        {
            if (B2Shapes.b2Shape_IsSensor(shapeId))
                return -1f;

            var state = (ClosestState)context;

            state.Hit = true;
            state.Fraction = fraction;
            state.Shape = shapeId;
            state.Point = point;
            state.Normal = normal;

            return fraction;
        }

        private static float AllCallback(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context)
        {
            if (B2Shapes.b2Shape_IsSensor(shapeId))
                return -1f;

            var state = (AllState)context;

            state.Results.Add(new CastHit
            {
                Collider = ColliderOf(shapeId),
                Point = new Vector2D<float>(point.X, point.Y),
                Normal = new Vector2D<float>(normal.X, normal.Y),
                Distance = fraction * state.Distance,
            });

            return 1f;
        }

        private static bool TryGetWorld(out B2WorldId world)
        {
            world = default;

            var system = SystemManager.Instance?.GetSystem<PhysicsSystem>();

            if (system == null)
                return false;

            world = system.WorldId;
            return B2Worlds.b2World_IsValid(world);
        }

        private static bool TryBegin(Vector2D<float> direction, float distance, out B2WorldId world, out B2Vec2 translation)
        {
            translation = default;

            if (!TryGetWorld(out world))
                return false;

            float length = direction.Length;

            if (length <= 0f || distance <= 0f)
                return false;

            float scale = distance / length;
            translation = new B2Vec2(direction.X * scale, direction.Y * scale);
            return true;
        }

        private static ClosestState ResetClosest()
        {
            _closest.Hit = false;
            _closest.Fraction = 0f;
            _closest.Shape = default;
            _closest.Point = default;
            _closest.Normal = default;
            return _closest;
        }

        private static AllState ResetAll(List<CastHit> results, float distance)
        {
            _all.Results = results;
            _all.Distance = distance;
            return _all;
        }

        private static bool TryBuildHit(ClosestState state, float distance, out CastHit hit)
        {
            if (!state.Hit)
            {
                hit = default;
                return false;
            }

            hit = new CastHit
            {
                Collider = ColliderOf(state.Shape),
                Point = new Vector2D<float>(state.Point.X, state.Point.Y),
                Normal = new Vector2D<float>(state.Normal.X, state.Normal.Y),
                Distance = state.Fraction * distance,
            };

            return true;
        }

        private static B2ShapeProxy BoxProxy(Vector2D<float> origin, Vector2D<float> size, float angle)
        {
            float halfX = size.X * 0.5f;
            float halfY = size.Y * 0.5f;

            float rad = angle * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);

            Span<B2Vec2> points = stackalloc B2Vec2[4];
            points[0] = Corner(-halfX, -halfY);
            points[1] = Corner(halfX, -halfY);
            points[2] = Corner(halfX, halfY);
            points[3] = Corner(-halfX, halfY);

            return B2Distances.b2MakeProxy(points, 4, 0f);

            B2Vec2 Corner(float x, float y) => new B2Vec2(
                origin.X + x * cos - y * sin,
                origin.Y + x * sin + y * cos);
        }

        private static Collider ColliderOf(B2ShapeId shapeId)
        {
            return B2Shapes.b2Shape_GetUserData(shapeId).oValue as Collider;
        }

        private static B2Vec2 ToB2(Vector2D<float> value)
        {
            return new B2Vec2(value.X, value.Y);
        }
    }
}