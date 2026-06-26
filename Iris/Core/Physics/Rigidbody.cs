using Box2D.NET;
using Iris.Physics;
using Silk.NET.Maths;

namespace Iris.Core
{
    public sealed class Rigidbody : Component
    {
        private Vector2D<float> _linearVelocity;
        private float _angularVelocity;
        private B2BodyId _id;

        public Vector2D<float> LinearVelocity
        {
            get
            {
                return _linearVelocity;
            }
            set
            {
                _linearVelocity = value;
                B2Bodies.b2Body_SetLinearVelocity(_id, new B2Vec2(value.X, value.Y));
            }
        }
        public float AngularVelocity
        {
            get
            {
                return _angularVelocity;
            }
            set
            {
                _angularVelocity = value;
                B2Bodies.b2Body_SetAngularVelocity(_id, value);
            }
        }

        protected override void OnAttached()
        {
            var sys = SystemManager.Instance.GetSystem<PhysicsSystem>();
            var def = B2Types.b2DefaultBodyDef();
            def.type = B2BodyType.b2_dynamicBody;
            def.gravityScale = 1;
            def.position = new B2Vec2(Transform.Position.X, Transform.Position.Y);
            def.rotation = B2MathFunction.b2MakeRot(Transform.Rotation);
            _id = sys.CreateBody(def);
        }

        public override void FixedUpdate()
        {
            var linVel = B2Bodies.b2Body_GetLinearVelocity(_id);
            _linearVelocity = new Vector2D<float>(linVel.X, linVel.Y);

            _angularVelocity = B2Bodies.b2Body_GetAngularVelocity(_id);

            var trm = B2Bodies.b2Body_GetTransform(_id);
            Transform.Position = new Vector2D<float>(trm.p.X, trm.p.Y);
            Transform.Rotation = B2MathFunction.b2Rot_GetAngle(trm.q);
        }

        internal B2BodyId GetBodyId()
        {
            return _id;
        }


        public override void Dispose()
        {
            B2Bodies.b2DestroyBody(_id);
        }
    }
}
