using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public class World : IDisposable
    {
        private readonly List<Actor> _actors = new();

        public static World CurrentWorld { get; internal set; }

        internal World()
        {
            CurrentWorld = this;
        }

        public Actor CreateActor()
        {
            var actor = new Actor();
            _actors.Add(actor);
            return actor;
        }

        internal void Update()
        {
            foreach (var item in _actors)
            {
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
            foreach (var item in _actors)
            {
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
            foreach (var item in _actors)
            {
                try
                {
                    item.LateUpdate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public void Dispose()
        {
            foreach (var item in _actors)
            {
                try
                {
                    item.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }

}
