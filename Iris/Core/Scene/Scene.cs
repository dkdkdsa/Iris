using Iris.Debugging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Iris.Core
{
    public sealed class Scene : IDisposable
    {
        private static readonly DebugChannel _log = Debug.Channel("Iris");

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
            for (int i = 0; i < _actors.Count; i++)
            {
                Actor item = _actors[i];
                try
                {
                    item.Update();
                }
                catch (Exception ex)
                {
                    _log.LogExceptionOnce(ex, item);
                }
            }
        }

        internal void FixedUpdate()
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                Actor item = _actors[i];
                try
                {
                    item.FixedUpdate();
                }
                catch (Exception ex)
                {
                    _log.LogExceptionOnce(ex, item);
                }
            }
        }

        internal void LateUpdate()
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                Actor item = _actors[i];
                try
                {
                    if (!item.DestroyFlag)
                        item.LateUpdate();
                    else
                        item.Dispose();
                }
                catch (Exception ex)
                {
                    _log.LogExceptionOnce(ex, item);
                }
            }

            _actors.RemoveAll(x => x.DestroyFlag);
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
                    _log.LogExceptionOnce(ex, item);
                }
            }

            _actors.Clear();
        }
    }
}