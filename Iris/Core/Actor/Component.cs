using Iris.Debugging;
using System;

namespace Iris.Core
{
    public abstract class Component : EngineObject, IDisposable
    {
        private bool _awakened;

        public Actor OwnerActor { get; private set; }
        public Transform Transform => OwnerActor.Transform;

        internal void Attach(Actor actor)
        {
            OwnerActor = actor;
            OnAttached();
        }

        internal void InvokeAwake()
        {
            if (_awakened)
                return;

            _awakened = true;

            try
            {
                Awake();
            }
            catch (Exception ex)
            {
                Debug.LogExceptionOnce(ex, this);
            }
        }

        protected T GetComponent<T>() where T : class => OwnerActor.GetComponent<T>();
        protected virtual void OnAttached() { }
        protected virtual void Awake() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void LateUpdate() { }
        public virtual void Dispose() { }

        public void Destroy()
        {
            OwnerActor.Destroy();
        }
    }
}
