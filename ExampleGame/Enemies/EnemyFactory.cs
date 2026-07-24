using Iris.Assets;
using Iris.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExampleGame.Enemies
{
    public class EnemyFactory : IFactory<Enemy, EnemyRequest>
    {
        public Enemy Create(EnemyRequest request)
        {
            var actor = Actor.Create();

            var renderer = actor.AddComponent<SpriteRenderer>();
            renderer.Sprite = Helper.GetAsset<Sprite>("Resources/DummyBox.png");

            var rigid = actor.AddComponent<Rigidbody>();
            rigid.Position = request.targetPosition;
            rigid.GravityScale = 0;

            actor.AddComponent<BoxCollider>();

            var enemy = actor.AddComponent<Enemy>();
            enemy.Init(rigid, request);

            return enemy;
        }
    }
}
