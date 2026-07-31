using Iris.Debugging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Iris.Core
{
    public sealed class Scene : IDisposable
    {
        private readonly List<Actor> _actors = new();

        public IReadOnlyList<Actor> Actors => _actors;

        public Actor CreateActor()
        {
            var actor = new Actor();
            _actors.Add(actor);
            return actor;
        }

        internal Actor CreateActorDeferred()
        {
            var actor = new Actor(deferAwake: true);
            _actors.Add(actor);
            return actor;
        }

        internal void Update()
        {
            int count = _actors.Count;

            for (int i = 0; i < count && i < _actors.Count; i++)
            {
                Actor item = _actors[i];
                try
                {
                    if (item.ActiveInHierarchy)
                        item.Update();
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce(ex, item);
                }
            }
        }

        internal void FixedUpdate()
        {
            int count = _actors.Count;

            for (int i = 0; i < count && i < _actors.Count; i++)
            {
                Actor item = _actors[i];
                try
                {
                    if (item.ActiveInHierarchy)
                        item.FixedUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce(ex, item);
                }
            }
        }

        internal void LateUpdate()
        {
            int count = _actors.Count;

            for (int i = 0; i < count && i < _actors.Count; i++)
            {
                Actor item = _actors[i];
                try
                {
                    if (item.DestroyFlag)
                        continue;

                    if (item.ActiveInHierarchy)
                        item.LateUpdate();
                    else
                        item.SweepComponents();
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce(ex, item);
                }
            }

            for (int i = _actors.Count - 1; i >= 0; i--)
            {
                Actor item = _actors[i];

                if (!item.DestroyFlag)
                    continue;

                _actors.RemoveAt(i);

                try
                {
                    item.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce(ex, item);
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                Actor item = _actors[i];
                try
                {
                    item.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogExceptionOnce(ex, item);
                }
            }

            _actors.Clear();
        }
    }
}