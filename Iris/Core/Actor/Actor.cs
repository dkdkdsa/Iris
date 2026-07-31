using Iris.Debugging;
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
        private bool _active = true;
        private bool _activeInHierarchy = true;

        public string Name { get; set; } = "Actor";
        public string Tag { get; set; } = "";
        public Transform Transform { get; private set; }

        public bool Active
        {
            get
            {
                return _active;
            }
            set
            {
                if (_active == value)
                    return;

                _active = value;
                RefreshActiveInHierarchy();
            }
        }

        public bool ActiveInHierarchy => _activeInHierarchy;

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

            Transform.MarkDirty();
            RefreshActiveInHierarchy();

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

            if (!_awake)
                return;

            component.InvokeAwake();
            component.RefreshEnabledInHierarchy();
        }

        private void RefreshActiveInHierarchy()
        {
            bool value = _active && (Parent?._activeInHierarchy ?? true);

            if (_activeInHierarchy == value)
                return;

            _activeInHierarchy = value;

            for (int i = 0; i < _components.Count; i++)
                _components[i].RefreshEnabledInHierarchy();

            for (int i = 0; i < _children.Count; i++)
                _children[i].RefreshActiveInHierarchy();
        }

        internal void Awake()
        {
            if (_awake)
                return;

            _awake = true;

            for (int i = 0; i < _components.Count; i++)
            {
                if (!_components[i].DestroyFlag)
                    _components[i].InvokeAwake();
            }

            for (int i = 0; i < _components.Count; i++)
                _components[i].RefreshEnabledInHierarchy();
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var compo = new T();
            AddComponent(compo);

            return compo;
        }

        public bool RemoveComponent(Component component)
        {
            if (component == null || component.DestroyFlag || !_components.Contains(component))
                return false;

            if (component == Transform)
            {
                Debug.LogOnce(LogLevel.Warning, "Transform cannot be removed from an actor.", this);
                return false;
            }

            component.MarkDestroy();
            return true;
        }

        public bool RemoveComponent<T>() where T : class
        {
            return RemoveComponent(GetComponent<T>() as Component);
        }

        public T GetComponent<T>() where T : class
        {
            for (int i = 0; i < _components.Count; i++)
            {
                var component = _components[i];

                if (!component.DestroyFlag && component is T match)
                    return match;
            }

            return null;
        }

        public T GetComponentInChildren<T>() where T : class
        {
            for (int i = 0; i < _children.Count; i++)
            {
                var component = _children[i].GetComponent<T>();
                if (component != null)
                    return component;

                component = _children[i].GetComponentInChildren<T>();
                if (component != null)
                    return component;
            }

            return null;
        }

        internal void Update()
        {
            int count = _components.Count;

            for (int i = 0; i < count && i < _components.Count; i++)
            {
                if (DestroyFlag)
                    break;

                var component = _components[i];

                if (!component.EnabledInHierarchy)
                    continue;

                try
                {
                    component.Update();
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce(ex, this);
                }
            }
        }

        internal void FixedUpdate()
        {
            int count = _components.Count;

            for (int i = 0; i < count && i < _components.Count; i++)
            {
                if (DestroyFlag)
                    break;

                var component = _components[i];

                if (!component.EnabledInHierarchy)
                    continue;

                try
                {
                    component.FixedUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce(ex, this);
                }
            }
        }

        internal void LateUpdate()
        {
            int count = _components.Count;

            for (int i = 0; i < count && i < _components.Count; i++)
            {
                if (DestroyFlag)
                    break;

                var component = _components[i];

                if (!component.EnabledInHierarchy)
                    continue;

                try
                {
                    component.LateUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce(ex, this);
                }
            }

            SweepComponents();
        }

        internal void SweepComponents()
        {
            for (int i = _components.Count - 1; i >= 0; i--)
            {
                var component = _components[i];

                if (!component.DestroyFlag)
                    continue;

                _components.RemoveAt(i);

                try
                {
                    component.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce(ex, this);
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

            for (int i = 0; i < _components.Count; i++)
            {
                try
                {
                    _components[i].MarkDestroy();
                    _components[i].Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce(ex, this);
                }
            }

            _components.Clear();
        }

        public static Actor Create()
        {
            var scene = SystemManager.Instance?.GetSystem<SceneSystem>()?.ActiveScene;

            if (scene == null)
                throw new InvalidOperationException(
                    "Cannot create actor: no active scene. Load a scene first (SceneManager.LoadScene), " +
                    "or start an empty one with SceneSystem.LoadScene(new Scene()).");

            return scene.CreateActor();
        }
    }
}
