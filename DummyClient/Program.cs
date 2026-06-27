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

            var actor = Actor.Create();
            var renderer = actor.AddComponent<TextureRenderer>();
            renderer.Texture = AssetManager.Load<ITexture>(@"C:\Users\bos94\Downloads\cog (1).png");
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

    protected override void OnAttached()
    {
        _player = GetComponent<AudioPlayer>();
    }

    public override void Update()
    {

        if (Input.GetKey(Silk.NET.SDL.KeyCode.KW))
        {
            Transform.Position += new Vector2D<float>(0, 1) * Time.DeltaTime;
        }
        if (Input.GetKey(Silk.NET.SDL.KeyCode.KA))
        {
            Transform.Position += new Vector2D<float>(-1, 0) * Time.DeltaTime;
        }
        if (Input.GetKey(Silk.NET.SDL.KeyCode.KS))
        {
            Transform.Position += new Vector2D<float>(0, -1) * Time.DeltaTime;
        }
        if (Input.GetKey(Silk.NET.SDL.KeyCode.KD))
        {
            Transform.Position += new Vector2D<float>(1, 0) * Time.DeltaTime;
        }

        if (Input.GetKey(Silk.NET.SDL.KeyCode.KR))
        {
            Transform.Rotation += 10 * Time.DeltaTime;
        }

        if (Input.GetKeyDown(Silk.NET.SDL.KeyCode.KG))
        {
            _player.Play();
        }
    }
}
