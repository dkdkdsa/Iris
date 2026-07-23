using ExampleGame.Bullets;
using ExampleGame.Enemies;
using ExampleGame.Player;
using Iris.Assets;
using Iris.Core;
using Iris.UI;
using Silk.NET.Maths;
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
            SystemManager.Instance.GetSystem<SceneSystem>().LoadScene(new Scene());

            CreateFactorys();
            CreateCamera();
            CreatePlayerObject();
            CreateHud();
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



        private void CreateHud()
        {
            var actor = Actor.Create();
            actor.Name = "HUD";

            var canvas = actor.AddComponent<Canvas>();
            canvas.ReferenceSize = new Vector2D<int>(800, 600);
            canvas.Match = 0.5f;

            var texture = Helper.GetAsset<ITexture>("Resources/DummyBox.png");

            var hearts = new List<UIObject>();

            for (int i = 0; i < 3; i++)
            {
                var heart = new UIObject
                {
                    Texture = texture,
                    Anchor = new Vector2D<float>(0f, 0f),
                    Position = new Vector2D<float>(16f + i * 40f, 16f),
                    Scale = new Vector2D<float>(2f, 2f),
                };

                hearts.Add(heart);
                canvas.AddUIObject(heart);
            }

            int visibleHearts = 3;
            var button = new Button
            {
                Texture = texture,
                Anchor = new Vector2D<float>(1f, 0f),
                Position = new Vector2D<float>(-16f, 16f),
                Scale = new Vector2D<float>(2f, 2f),
            };

            var font = AssetManager.Load<IFont>(@"C:\Windows\Fonts\malgun.ttf");

            var label = new TextUIObject
            {
                Font = font,
                Text = "하트: 3개",
                FontSize = 28f,
                Color = new Color(255, 220, 80, 255),
                Anchor = new Vector2D<float>(0.5f, 0f),
                Position = new Vector2D<float>(0f, 16f),
            };

            button.Clicked += () =>
            {
                visibleHearts = visibleHearts <= 0 ? 3 : visibleHearts - 1;

                for (int i = 0; i < hearts.Count; i++)
                    hearts[i].Visible = i < visibleHearts;

                label.Text = $"하트: {visibleHearts}개";
                Console.WriteLine($"[ExampleGame] 버튼 클릭 - 하트 {visibleHearts}개");
            };

            canvas.AddUIObject(button);
            canvas.AddUIObject(label);

            canvas.AddUIObject(new UIObject
            {
                Texture = texture,
                Anchor = new Vector2D<float>(0f, 1f),
                Position = new Vector2D<float>(16f, -16f),
                Scale = new Vector2D<float>(2f, 2f),
            });

            canvas.AddUIObject(new UIObject
            {
                Texture = texture,
                Anchor = new Vector2D<float>(0.5f, 1f),
                Position = new Vector2D<float>(0f, -16f),
                Scale = new Vector2D<float>(12f, 1f),
            });
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
