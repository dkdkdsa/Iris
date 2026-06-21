using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public abstract class ActionScript
    {

        internal void Init()
        {
        }

        public virtual void OnCreate() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
    }
}
