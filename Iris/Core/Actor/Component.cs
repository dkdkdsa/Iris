using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public abstract class Component : IDisposable
    {
        public Actor OwnerActor { get; private set; }
        public Transform Transform => OwnerActor.Transform;

        internal void Attach(Actor actor)
        {
            OwnerActor = actor;
            OnAttached();
        }

        protected T GetComponent<T>() where T : Component => OwnerActor.GetComponent<T>();
        protected virtual void OnAttached() { }
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
