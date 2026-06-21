using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public sealed class Actor
    {
        private List<Component> _components;

        public void AddComponent(Component component)
        {
            component.Attach(this);
            _components.Add(component);
        }

        internal void Update()
        {
            foreach (Component component in _components)
            {
                component.Update();
            }
        }

        internal void FixedUpdate()
        {
            foreach (Component component in _components)
            {
                component.FixedUpdate();
            }
        }
    }
}