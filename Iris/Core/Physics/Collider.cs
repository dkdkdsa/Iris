using Box2D.NET;
using Iris.Debugging;
using Iris.Physics;
using Silk.NET.Maths;
using System;

namespace Iris.Core
{
    public abstract class Collider : Component
    {
        public event Action<Collider> OnSensorBegin;
        public event Action<Collider> OnSensorEnd;
        public event Action<Collider> OnContactBegin;
        public event Action<Collider> OnContactEnd;

        private Rigidbody _rigid;
        private bool _isSensor;
        private float _friction = 0.6f;

        private B2BodyId _staticBody;
        private Vector2D<float> _appliedPosition;
        private float _appliedRotation;

        protected B2ShapeId shapeId;

        public bool IsStatic => B2Worlds.b2Body_IsValid(_staticBody);

        protected Rigidbody rigid
        {
            get
            {
                if (_rigid == null)
                    _rigid = GetComponent<Rigidbody>();

                return _rigid;
            }
        }

        [Iris.Attributes.Show]
        public bool IsSensor
        {
            get
            {
                return _isSensor;
            }
            set
            {
                _isSensor = value;
                ApplySensor();
            }
        }

        [Iris.Attributes.Show]
        public float Friction
        {
            get
            {
                return B2Worlds.b2Shape_IsValid(shapeId)
                    ? B2Shapes.b2Shape_GetFriction(shapeId)
                    : _friction;
            }
            set
            {
                _friction = value;
                ApplyFriction();
            }
        }

        protected virtual void ApplyFriction()
        {
            if (B2Worlds.b2Shape_IsValid(shapeId))
                B2Shapes.b2Shape_SetFriction(shapeId, _friction);
        }

        protected virtual void ApplySensor()
        {
            if (!EnabledInHierarchy)
                return;

            RebuildShape();
        }

        protected override void OnEnable()
        {
            RebuildShape();

            if (B2Worlds.b2Body_IsValid(_staticBody))
                B2Bodies.b2Body_Enable(_staticBody);
        }

        protected override void OnDisable()
        {
            if (B2Worlds.b2Shape_IsValid(shapeId))
                B2Shapes.b2DestroyShape(shapeId, false);

            shapeId = default;

            if (B2Worlds.b2Body_IsValid(_staticBody))
                B2Bodies.b2Body_Disable(_staticBody);
        }

        private void RebuildShape()
        {
            var body = ResolveBody();

            if (!B2Worlds.b2Body_IsValid(body))
                return;

            if (B2Worlds.b2Shape_IsValid(shapeId))
                B2Shapes.b2DestroyShape(shapeId, false);

            shapeId = CreateShape(body);
            B2Shapes.b2Shape_SetUserData(shapeId, new B2UserData(this));
        }

        private B2BodyId ResolveBody()
        {
            var body = rigid?.GetBodyId() ?? default;

            if (B2Worlds.b2Body_IsValid(body))
            {
                DestroyStaticBody();
                return body;
            }

            if (B2Worlds.b2Body_IsValid(_staticBody))
                return _staticBody;

            var system = SystemManager.Instance?.GetSystem<PhysicsSystem>();

            if (system == null)
            {
                Debug.LogOnce(LogLevel.Warning,
                    $"{GetType().Name} requires a PhysicsSystem; no shape was created.", this);
                return default;
            }

            _appliedPosition = Transform.Position;
            _appliedRotation = Transform.Rotation;

            var def = B2Types.b2DefaultBodyDef();
            def.type = B2BodyType.b2_staticBody;
            def.position = new B2Vec2(_appliedPosition.X, _appliedPosition.Y);
            def.rotation = Rigidbody.ToBodyRotation(_appliedRotation);

            _staticBody = system.CreateBody(def);
            return _staticBody;
        }

        public override void FixedUpdate()
        {
            if (!B2Worlds.b2Body_IsValid(_staticBody))
                return;

            if (B2Worlds.b2Body_IsValid(rigid?.GetBodyId() ?? default))
            {
                RebuildShape();
                return;
            }

            var position = Transform.Position;
            float rotation = Transform.Rotation;

            if (position == _appliedPosition && rotation == _appliedRotation)
                return;

            _appliedPosition = position;
            _appliedRotation = rotation;

            B2Bodies.b2Body_SetTransform(_staticBody,
                new B2Vec2(position.X, position.Y), Rigidbody.ToBodyRotation(rotation));
        }

        private void DestroyStaticBody()
        {
            if (B2Worlds.b2Body_IsValid(_staticBody))
                B2Bodies.b2DestroyBody(_staticBody);

            _staticBody = default;
            shapeId = default;
        }

        protected abstract B2ShapeId CreateShape(B2BodyId id);


        internal void NotifySensorEvent(Collider other, bool enter)
        {
            if (enter)
                OnSensorBegin?.Invoke(other);
            else
                OnSensorEnd?.Invoke(other);
        }

        internal void NotifyContactEvent(Collider other, bool enter)
        {
            if (enter)
                OnContactBegin?.Invoke(other);
            else
                OnContactEnd?.Invoke(other);
        }

        public override void Dispose()
        {
            if (B2Worlds.b2Shape_IsValid(shapeId))
                B2Shapes.b2DestroyShape(shapeId, false);

            shapeId = default;
            DestroyStaticBody();
        }
    }
}