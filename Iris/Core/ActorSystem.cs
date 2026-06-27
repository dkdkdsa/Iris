using System;
using System.Collections.Generic;

namespace Iris.Core
{
    public sealed class ActorSystem : SystemBase
    {
        private readonly List<Actor> _actors = new(); //나중에 씬 핸들링 방식으로 바꾸기

        public Actor CreateActor()
        {
            var actor = new Actor();
            _actors.Add(actor);
            return actor;
        }

        public override void Update()
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


        public override void FixedUpdate()
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

        public override void LateUpdate()
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

            _actors.RemoveAll(x => x.DestroyFlag);
        }

        public override void Dispose()
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

            _actors.Clear();
        }
    }
}
