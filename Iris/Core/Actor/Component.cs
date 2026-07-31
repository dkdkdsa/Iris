using Iris.Debugging;
using System;

namespace Iris.Core
{
    public abstract class Component : EngineObject, IDisposable
    {
        private bool _awakened;
        private bool _enabled = true;
        private bool _enabledInHierarchy;

        public Actor OwnerActor { get; private set; }
        public Transform Transform => OwnerActor.Transform;
        public bool DestroyFlag { get; private set; }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled == value)
                    return;

                _enabled = value;
                RefreshEnabledInHierarchy();
            }
        }

        public bool EnabledInHierarchy => _enabledInHierarchy;

        internal void MarkDestroy()
        {
            if (DestroyFlag)
                return;

            DestroyFlag = true;
            RefreshEnabledInHierarchy();
        }

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

        internal void RefreshEnabledInHierarchy()
        {
            bool target = _awakened && _enabled && !DestroyFlag && (OwnerActor?.ActiveInHierarchy ?? false);

            if (_enabledInHierarchy == target)
                return;

            _enabledInHierarchy = target;

            try
            {
                if (target)
                    OnEnable();
                else
                    OnDisable();
            }
            catch (Exception ex)
            {
                Debug.LogExceptionOnce(ex, this);
            }
        }

        protected T GetComponent<T>() where T : class => OwnerActor.GetComponent<T>();
        protected virtual void OnAttached() { }
        protected virtual void Awake() { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void LateUpdate() { }
        public virtual void Dispose() { }

        public void Destroy()
        {
            OwnerActor?.RemoveComponent(this);
        }
    }
}
