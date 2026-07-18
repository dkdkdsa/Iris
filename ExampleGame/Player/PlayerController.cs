using ExampleGame.Bullets;
using Iris.Core;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExampleGame.Player
{
    public class PlayerController : Component
    {
        private Rigidbody _rigid;
        private IFactory<Bullet, BulletRequest> _factory;

        protected override void OnAttached()
        {
            _rigid = GetComponent<Rigidbody>();
            _factory = Factorys.GetFactory<Bullet, BulletRequest>();
        }

        public override void Update()
        {
            Move();
            Attack();
        }

        private void Attack()
        {
            if (Input.GetMouseButtonDown(MouseButton.Left))
            {
                var target = Camera.Main.ScreenToWorld(Input.MousePosition) - Transform.Position;
                _factory.Create(new BulletRequest
                {
                    dir = Vector2D.Normalize(target),
                    speed = 3,
                    lifetime = 3
                });
            }
        }

        private void Move()
        {
            var vector = new Vector2D<float>();

            if (Input.GetKey(KeyCode.W))
            {
                vector.Y += 1;
            }
            if (Input.GetKey(KeyCode.A))
            {
                vector.X -= 1;
            }
            if (Input.GetKey(KeyCode.S))
            {
                vector.Y -= 1;
            }
            if (Input.GetKey(KeyCode.D))
            {
                vector.X += 1;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                Destroy();
            }


            _rigid.LinearVelocity = vector.Length > 0 ? Vector2D.Normalize(vector) : vector;
        }
    }
}
