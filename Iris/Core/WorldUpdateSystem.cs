using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    internal class WorldUpdateSystem : SystemBase
    {
        public override void Update()
        {
            World.CurrentWorld.Update();
        }

        public override void FixedUpdate()
        {
            World.CurrentWorld.FixedUpdate();
        }

        public override void LateUpdate()
        {
            World.CurrentWorld.LateUpdate();
        }

        public override void Dispose()
        {
            World.CurrentWorld.Dispose();
        }
    }
}
