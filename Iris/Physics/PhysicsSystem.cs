using Box2D.NET;
using Iris.Core;
using System.Collections.Generic;

namespace Iris.Physics
{
    internal class PhysicsSystem : SystemBase
    {
        private readonly B2WorldId _worldId;
        private readonly List<Rigidbody> _bodies = new();

        public PhysicsSystem()
        {
            B2WorldDef def = B2Types.b2DefaultWorldDef();
            def.gravity = new B2Vec2(0f, -9.81f);

            _worldId = B2Worlds.b2CreateWorld(def);

            Order = 100;
        }

        internal void Register(Rigidbody body)
        {
            if (body != null && !_bodies.Contains(body))
                _bodies.Add(body);
        }

        internal void Unregister(Rigidbody body)
        {
            _bodies.Remove(body);
        }

        public override void FixedUpdate()
        {
            B2Worlds.b2World_Step(_worldId, Time.FixedTimeStep, 4);

            for (int i = 0; i < _bodies.Count; i++)
                _bodies[i].SyncFromBody();

            {
                var evts = B2Worlds.b2World_GetContactEvents(_worldId);

                for (int i = 0; i < evts.beginCount; i++)
                {
                    var item = evts.beginEvents[i];

                    var a = B2Shapes.b2Shape_GetUserData(item.shapeIdA).oValue as Collider;
                    var b = B2Shapes.b2Shape_GetUserData(item.shapeIdB).oValue as Collider;

                    a?.NotifyContactEvent(b, true);
                    b?.NotifyContactEvent(a, true);
                }

                for (int i = 0; i < evts.endCount; i++)
                {
                    var item = evts.endEvents[i];

                    var a = B2Shapes.b2Shape_GetUserData(item.shapeIdA).oValue as Collider;
                    var b = B2Shapes.b2Shape_GetUserData(item.shapeIdB).oValue as Collider;

                    a?.NotifyContactEvent(b, false);
                    b?.NotifyContactEvent(a, false);
                }
            }

            {
                var evts = B2Worlds.b2World_GetSensorEvents(_worldId);

                for (int i = 0; i < evts.beginCount; i++)
                {
                    B2SensorBeginTouchEvent item = evts.beginEvents[i];
                    var sensor = B2Shapes.b2Shape_GetUserData(item.sensorShapeId).oValue as Collider;
                    var other = B2Shapes.b2Shape_GetUserData(item.visitorShapeId).oValue as Collider;
                    if (sensor != null)
                    {
                        sensor.NotifySensorEvent(other, true);
                    }
                }

                for (int i = 0; i < evts.endCount; i++)
                {
                    B2SensorEndTouchEvent item = evts.endEvents[i];
                    var sensor = B2Shapes.b2Shape_GetUserData(item.sensorShapeId).oValue as Collider;
                    var other = B2Shapes.b2Shape_GetUserData(item.visitorShapeId).oValue as Collider;
                    if (sensor != null)
                    {
                        sensor.NotifySensorEvent(other, false);
                    }
                }
            }
        }

        internal B2WorldId WorldId => _worldId;

        public B2BodyId CreateBody(in B2BodyDef def)
        {
            return B2Bodies.b2CreateBody(_worldId, def);
        }
    }
}
