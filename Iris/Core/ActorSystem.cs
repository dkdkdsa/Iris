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


        public override void FixedUpdate()
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

        public override void LateUpdate()
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

        public override void Dispose()
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
