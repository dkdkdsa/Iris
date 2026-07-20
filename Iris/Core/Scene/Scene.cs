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
                    Console.WriteLine(ex);
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
                    Console.WriteLine(ex);
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
                    Console.WriteLine(ex);
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
                    Console.WriteLine(ex);
                }
            }

            _actors.Clear();
        }
    }
}