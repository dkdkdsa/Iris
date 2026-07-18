using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public sealed class Scene : IDisposable
    {
        private readonly List<Actor> _actors = new();

        internal void AddActor(Actor actor)
        {
            _actors.Add(actor);
        }

        public void Dispose()
        {
            foreach (var actor in _actors)
                actor.Dispose();
        }
    }
}
