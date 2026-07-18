using Iris.Core;

namespace ExampleGame.Enemies
{
    public class Enemy : Component
    {
        private Rigidbody _rigid;
        private Transform _target;
        private float _speed;

        public void Init(Rigidbody rigid, EnemyRequest req)
        {
            _rigid = rigid;
            _target = req.target;
            _speed = req.speed;
        }

        public override void Update()
        {
            base.Update();
        }
    }
}
