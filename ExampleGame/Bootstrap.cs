using ExampleGame.Bullets;
using ExampleGame.Enemies;
using ExampleGame.Player;
using Iris.Assets;
using Iris.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExampleGame
{
    public class Bootstrap
    {
        public void Start()
        {
            CreateFactorys();
            CreateCamera();
            CreatePlayerObject();
        }

        private void CreateCamera()
        {
            Actor.Create().AddComponent<Camera>();
        }

        private void CreateFactorys()
        {
            Factorys.Register(new BulletFactory());
            Factorys.Register(new EnemyFactory());
        }



        private void CreatePlayerObject()
        {
            var player = Actor.Create();

            var render = player.AddComponent<TextureRenderer>();
            render.Texture = Helper.GetAsset<ITexture>("Resources/DummyBox.png");
            render.PixelPerUnit = 16;


            var rigid = player.AddComponent<Rigidbody>();
            rigid.GravityScale = 0;

            player.AddComponent<BoxCollider>();

            player.AddComponent<PlayerController>();
        }
    }
}
