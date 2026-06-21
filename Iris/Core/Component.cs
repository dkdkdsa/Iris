using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public abstract class Component
    {
        public Actor OwnerActor { get; private set; }

        internal void Attach(Actor actor)
        {
            OwnerActor = actor;
            OnAttached();
        }

        protected virtual void OnAttached() { }
        public virtual void Update() { }
        public  virtual void FixedUpdate() { }
    }
}
