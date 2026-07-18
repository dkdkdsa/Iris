using Iris.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExampleGame.Bullets
{
    public class Bullet : Component
    {
        private Rigidbody _rigid;
        private float _lifetime;

        public void Init(Rigidbody rigid, BulletRequest req)
        {
            _rigid = rigid;
            _rigid.LinearVelocity = req.dir * req.speed;
            _lifetime = req.lifetime;
        }

        public override void Update()
        {
            _lifetime -= Time.DeltaTime;

            if (_lifetime < 0)
            {
                Destroy();
            }
        }

    }
}
