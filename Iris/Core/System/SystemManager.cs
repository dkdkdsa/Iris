using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Iris.Core
{
    public class SystemManager : IDisposable
    {
        public static SystemManager Instance { get; private set; }
        private List<SystemBase> _systems = new();

        internal SystemManager()
        {
            Instance = this;
        }

        public void CreateSystem<T>() where T : SystemBase, new()
        {
            var system = new T();
            AddSystem(system);
        }

        public void AddSystem(SystemBase system)
        {
            _systems.Add(system);
            _systems.Sort((s1, s2) => s1.Order.CompareTo(s2.Order));
        }

        public T GetSystem<T>() where T : SystemBase
        {
            return _systems.OfType<T>().FirstOrDefault();
        }

        internal void Update()
        {
            foreach (var system in _systems)
            {
                system.Update();
            }
        }

        internal void FixedUpdate()
        {
            foreach (var system in _systems)
            {
                system.FixedUpdate();
            }
        }

        public void Dispose()
        {
            foreach (var system in _systems)
            {
                system.Dispose();
            }
        }
    }
}
