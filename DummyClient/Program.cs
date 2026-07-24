using Iris;
using Iris.Assets;
using Iris.Core;
using Iris.Platform;
using Iris.Platform.SDL;
using Silk.NET.Maths;
using System.IO.Pipes;

namespace DummyClient
{
    internal class Program
    {

        public static Engine engine;
        static void Main(string[] args)
        {
            engine = new Engine(new DefaultPlatform());
            engine.OnStart += HandleStart;
            engine.Run(new Iris.Core.WindowConfig
            {
                width = 800,
                height = 600,
                title = "_"
            });
        }

        private static void HandleStart()
        {
            Console.WriteLine(1);

            Actor.Create().AddComponent<Camera>();

            var actor = Actor.Create();
            var renderer = actor.AddComponent<SpriteRenderer>();
            renderer.Sprite = AssetManager.Load<Sprite>(@"C:\Users\bos94\Downloads\cog (1).png");
            renderer.PixelPerUnit = 512;

            var audio = actor.AddComponent<AudioPlayer>();
            audio.Clip = AssetManager.Load<IAudioClip>(@"C:\Users\bos94\Downloads\laserShoot.wav");

            actor.AddComponent<Rigidbody>();
            actor.AddComponent<BoxCollider>();

            actor.AddComponent<TestCompo>();
        }
    }
}


public class TestCompo : Component
{
    private AudioPlayer _player;

    protected override void Awake()
    {
        _player = GetComponent<AudioPlayer>();
    }

    public override void Update()
    {

        if (Input.GetKey(KeyCode.W))
        {
            Transform.Position += new Vector2D<float>(0, 1) * Time.DeltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            Transform.Position += new Vector2D<float>(-1, 0) * Time.DeltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            Transform.Position += new Vector2D<float>(0, -1) * Time.DeltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            Transform.Position += new Vector2D<float>(1, 0) * Time.DeltaTime;
        }

        if (Input.GetKey(KeyCode.R))
        {
            Transform.Rotation += 10 * Time.DeltaTime;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            _player.Play();
        }
    }
}
