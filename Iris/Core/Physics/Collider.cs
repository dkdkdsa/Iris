using Box2D.NET;
using System;

namespace Iris.Core
{
    public abstract class Collider : Component
    {
        public event Action<Collider> OnSencerBegin;
        public event Action<Collider> OnSencerEnd;
        public event Action<Collider> OnContactBegin;
        public event Action<Collider> OnContactEnd;

        private Rigidbody _rigid;
        private bool _isSencer;

        protected B2ShapeId shapeId;

        protected Rigidbody rigid
        {
            get
            {
                if (_rigid == null)
                    _rigid = GetComponent<Rigidbody>();

                return _rigid;
            }
        }

        public bool IsSensor
        {
            get
            {
                return _isSencer;
            }
            set
            {
                _isSencer = value;
                ApplySensor();
            }
        }

        protected virtual void ApplySensor()
        {
            if (OwnerActor == null)
                return;

            if (B2Worlds.b2Shape_IsValid(shapeId))
                B2Shapes.b2DestroyShape(shapeId, false);

            shapeId = CreateShape(rigid.GetBodyId());
            B2Shapes.b2Shape_SetUserData(shapeId, new B2UserData(this));
        }


        protected override void OnAttached()
        {
            shapeId = CreateShape(rigid.GetBodyId());
            B2Shapes.b2Shape_SetUserData(shapeId, new B2UserData(this));
        }

        protected abstract B2ShapeId CreateShape(B2BodyId id);


        internal void NotifySencerEvent(Collider other, bool enter)
        {
            if (enter)
                OnSencerBegin?.Invoke(other);
            else
                OnSencerEnd?.Invoke(other);
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
            {
                B2Shapes.b2DestroyShape(shapeId, false);
            }
        }
    }
}