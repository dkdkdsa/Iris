using Box2D.NET;
using Iris.Attributes;
using Iris.Physics;
using Silk.NET.Maths;

namespace Iris.Core
{
    public sealed class Rigidbody : Component
    {
        private Vector2D<float> _linearVelocity;
        private float _angularVelocity;
        private float _gravityScale = 1f;
        private float _linearDamping;
        private float _angularDamping;
        private B2BodyId _id;

        [Show]
        public Vector2D<float> LinearVelocity
        {
            get
            {
                if (!B2Worlds.b2Body_IsValid(_id))
                    return _linearVelocity;

                var velocity = B2Bodies.b2Body_GetLinearVelocity(_id);
                return new Vector2D<float>(velocity.X, velocity.Y);
            }
            set
            {
                _linearVelocity = value;

                if (B2Worlds.b2Body_IsValid(_id))
                    B2Bodies.b2Body_SetLinearVelocity(_id, new B2Vec2(value.X, value.Y));
            }
        }

        public Vector2D<float> Position
        {
            get
            {
                return Transform.Position;
            }
            set
            {
                if (B2Worlds.b2Body_IsValid(_id))
                    B2Bodies.b2Body_SetTransform(_id, new B2Vec2(value.X, value.Y), B2MathFunction.b2MakeRot(Transform.Rotation));

                Transform.Position = value;
            }
        }

        [Show]
        public float AngularVelocity
        {
            get
            {
                return B2Worlds.b2Body_IsValid(_id)
                    ? B2Bodies.b2Body_GetAngularVelocity(_id)
                    : _angularVelocity;
            }
            set
            {
                _angularVelocity = value;

                if (B2Worlds.b2Body_IsValid(_id))
                    B2Bodies.b2Body_SetAngularVelocity(_id, value);
            }
        }

        [Show]
        public float GravityScale
        {
            get
            {
                return _gravityScale;
            }
            set
            {
                _gravityScale = value;

                if (B2Worlds.b2Body_IsValid(_id))
                    B2Bodies.b2Body_SetGravityScale(_id, value);
            }
        }

        [Show]
        public float LinearDamping
        {
            get
            {
                return B2Worlds.b2Body_IsValid(_id)
                    ? B2Bodies.b2Body_GetLinearDamping(_id)
                    : _linearDamping;
            }
            set
            {
                _linearDamping = value;

                if (B2Worlds.b2Body_IsValid(_id))
                    B2Bodies.b2Body_SetLinearDamping(_id, value);
            }
        }

        [Show]
        public float AngularDamping
        {
            get
            {
                return B2Worlds.b2Body_IsValid(_id)
                    ? B2Bodies.b2Body_GetAngularDamping(_id)
                    : _angularDamping;
            }
            set
            {
                _angularDamping = value;

                if (B2Worlds.b2Body_IsValid(_id))
                    B2Bodies.b2Body_SetAngularDamping(_id, value);
            }
        }

        public void AddForce(Vector2D<float> force, ForceMode mode = ForceMode.Force)
        {
            if (!B2Worlds.b2Body_IsValid(_id))
                return;

            var value = new B2Vec2(force.X, force.Y);

            if (mode == ForceMode.Impulse)
                B2Bodies.b2Body_ApplyLinearImpulseToCenter(_id, value, true);
            else
                B2Bodies.b2Body_ApplyForceToCenter(_id, value, true);
        }

        public void AddForceAtPosition(Vector2D<float> force, Vector2D<float> worldPoint, ForceMode mode = ForceMode.Force)
        {
            if (!B2Worlds.b2Body_IsValid(_id))
                return;

            var value = new B2Vec2(force.X, force.Y);
            var point = new B2Vec2(worldPoint.X, worldPoint.Y);

            if (mode == ForceMode.Impulse)
                B2Bodies.b2Body_ApplyLinearImpulse(_id, value, point, true);
            else
                B2Bodies.b2Body_ApplyForce(_id, value, point, true);
        }

        public void AddTorque(float torque, ForceMode mode = ForceMode.Force)
        {
            if (!B2Worlds.b2Body_IsValid(_id))
                return;

            if (mode == ForceMode.Impulse)
                B2Bodies.b2Body_ApplyAngularImpulse(_id, torque, true);
            else
                B2Bodies.b2Body_ApplyTorque(_id, torque, true);
        }

        protected override void OnAttached()
        {
            var sys = SystemManager.Instance.GetSystem<PhysicsSystem>();
            var def = B2Types.b2DefaultBodyDef();
            def.type = B2BodyType.b2_dynamicBody;
            def.gravityScale = _gravityScale;
            def.linearDamping = _linearDamping;
            def.angularDamping = _angularDamping;
            def.linearVelocity = new B2Vec2(_linearVelocity.X, _linearVelocity.Y);
            def.angularVelocity = _angularVelocity;
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
            if (B2Worlds.b2Body_IsValid(_id))
                B2Bodies.b2DestroyBody(_id);
        }
    }
}
