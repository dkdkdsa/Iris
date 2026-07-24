using Iris.Assets;
using Iris.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExampleGame.Bullets
{
    public class BulletFactory : IFactory<Bullet, BulletRequest>
    {
        public Bullet Create(BulletRequest request)
        {
            Actor actor = Actor.Create();

            var render = actor.AddComponent<SpriteRenderer>();
            render.Sprite = GetAsset<Sprite>("Resources/DummyBox.png");
            render.PixelPerUnit = 32;

            var rigid = actor.AddComponent<Rigidbody>();
            rigid.GravityScale = 0;

            var col = actor.AddComponent<BoxCollider>();
            col.Size /= 2;
            col.IsSensor = true;

            var blt = actor.AddComponent<Bullet>();
            blt.Init(rigid, request);

            return blt;
        }

        private T GetAsset<T>(string path) where T : IAsset
        {
            return AssetManager.Load<T>(Path.Combine(AppContext.BaseDirectory, path));
        }
    }
}