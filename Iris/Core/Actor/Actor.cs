using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public sealed class Actor : IDisposable
    {
        public Transform Transform { get; private set; }
        private List<Component> _components = new();

        internal Actor()
        {
            Transform = AddComponent<Transform>();
        }

        public void AddComponent(Component component)
        {
            component.Attach(this);
            _components.Add(component);
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var compo = new T();
            AddComponent(compo);

            return compo;
        }

        internal void Update()
        {
            foreach (Component component in _components)
            {
                try
                {
                    component.Update();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

            }
        }

        internal void FixedUpdate()
        {
            foreach (Component component in _components)
            {
                try
                {
                    component.FixedUpdate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        internal void LateUpdate()
        {
            foreach(Component component in _components)
            {
                try
                {
                    component.LateUpdate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public void Dispose()
        {
            foreach (Component component in _components)
            {
                try
                {
                    component.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            _components.Clear();
        }
    }
}