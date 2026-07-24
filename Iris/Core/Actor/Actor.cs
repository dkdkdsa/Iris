using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace Iris.Core
{
    public sealed class Actor : EngineObject, IDisposable
    {
        private List<Component> _components = new();
        private readonly List<Actor> _children = new();
        private bool _awake;

        public string Name { get; set; } = "Actor";
        public Transform Transform { get; private set; }
        public bool DestroyFlag { get; private set; }

        public Actor Parent { get; private set; }
        public IReadOnlyList<Actor> Children => _children;

        internal Actor(bool deferAwake = false)
        {
            _awake = !deferAwake;
            Transform = AddComponent<Transform>();
        }

        public void SetParent(Actor parent, bool worldPositionStays = true)
        {
            if (parent == this || Parent == parent)
                return;

            for (var ancestor = parent; ancestor != null; ancestor = ancestor.Parent)
            {
                if (ancestor == this)
                    return;
            }

            Vector2D<float> position = default;
            float rotation = 0f;
            Vector2D<float> scale = default;

            if (worldPositionStays)
            {
                position = Transform.Position;
                rotation = Transform.Rotation;
                scale = Transform.Scale;
            }

            Parent?._children.Remove(this);
            Parent = parent;
            parent?._children.Add(this);

            if (worldPositionStays)
            {
                Transform.Scale = scale;
                Transform.Rotation = rotation;
                Transform.Position = position;
            }
        }

        public void AddComponent(Component component)
        {
            component.Attach(this);
            _components.Add(component);

            if (_awake)
                component.InvokeAwake();
        }

        internal void Awake()
        {
            if (_awake)
                return;

            _awake = true;

            for (int i = 0; i < _components.Count; i++)
                _components[i].InvokeAwake();
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var compo = new T();
            AddComponent(compo);

            return compo;
        }

        public T GetComponent<T>() where T : class
        {
            for (int i = 0; i < _components.Count; i++)
            {
                if (_components[i] is T match)
                    return match;
            }

            return null;
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
            if (DestroyFlag)
                return;

            DestroyFlag = true;

            foreach (var child in _children)
                child.Destroy();
        }

        public void Dispose()
        {
            Parent?._children.Remove(this);
            Parent = null;

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
