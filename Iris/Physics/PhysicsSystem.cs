using Box2D.NET;
using Iris.Core;

namespace Iris.Physics
{
    internal class PhysicsSystem : SystemBase
    {
        private readonly B2WorldId _worldId;

        public PhysicsSystem()
        {
            B2WorldDef def = B2Types.b2DefaultWorldDef();
            def.gravity = new B2Vec2(0f, -9.81f);

            _worldId = B2Worlds.b2CreateWorld(def);

            Order = -100;
        }

        public override void FixedUpdate()
        {
            B2Worlds.b2World_Step(_worldId, Time.FixedTimeStep, 4);

            //contact
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

            //sencerEvents
            {
                var evts = B2Worlds.b2World_GetSensorEvents(_worldId);

                for (int i = 0; i < evts.beginCount; i++)
                {
                    B2SensorBeginTouchEvent item = evts.beginEvents[i];
                    var sencer = B2Shapes.b2Shape_GetUserData(item.sensorShapeId).oValue as Collider;
                    var other = B2Shapes.b2Shape_GetUserData(item.visitorShapeId).oValue as Collider;
                    if (sencer != null)
                    {
                        sencer.NotifySencerEvent(other, true);
                    }
                }

                for (int i = 0; i < evts.endCount; i++)
                {
                    B2SensorEndTouchEvent item = evts.endEvents[i];
                    var sencer = B2Shapes.b2Shape_GetUserData(item.sensorShapeId).oValue as Collider;
                    var other = B2Shapes.b2Shape_GetUserData(item.visitorShapeId).oValue as Collider;
                    if (sencer != null)
                    {
                        sencer.NotifySencerEvent(other, false);
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
