using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public sealed class Actor : EngineObject, IDisposable
    {
        private List<Component> _components = new();
        public string Name { get; set; } = "Actor";
        public Transform Transform { get; private set; }
        public bool DestroyFlag { get; private set; }

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

        public T GetComponent<T>() where T : Component
        {
            return _components.Find(x => x.GetType() == typeof(T)) as T;
        }

        internal void Update()
        {
            foreach (Component component in _components)
            {
                try
                {
                    if (DestroyFlag)
                        break;
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
                    if (DestroyFlag)
                        break;
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
                    if (DestroyFlag)
                        break;
                    component.LateUpdate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public void Destroy()
        {
            DestroyFlag = true;
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

        public static Actor Create()
        {
            return SystemManager.Instance.GetSystem<SceneSystem>().ActiveScene.CreateActor();
        }
    }
}