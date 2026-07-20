using System;

namespace Iris.Core
{
    public abstract class SystemBase : EngineObject, IDisposable
    {
        public int Order { get; protected set; }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void LateUpdate() { }
        public virtual void Dispose() { }
    }
}
