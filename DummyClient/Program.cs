using Iris;
using Iris.Core;
using Iris.Platform.SDL;
using Silk.NET.Maths;

namespace DummyClient
{
    internal class Program
    {

        public static Engine engine;
        static void Main(string[] args)
        {
            engine = new Engine(new SDLPlatform());
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

            var actor = World.CurrentWorld.CreateActor();
            var renderer = actor.AddComponent<TextureRenderer>();
            renderer.Texture = engine.TextureMgr.Load(@"C:\Users\bos94\Downloads\cog (1).png");
            actor.AddComponent<TestCompo>();
        }
    }
}

public class TestCompo : Component
{
    public override void Update()
    {

        if (Input.GetKey(Silk.NET.SDL.KeyCode.KW))
        {
            OwnerActor.Transform.Position += new Vector2D<float>(0, 100) * Time.DeltaTime;
        }
        if (Input.GetKey(Silk.NET.SDL.KeyCode.KA))
        {
            OwnerActor.Transform.Position += new Vector2D<float>(-100, 0) * Time.DeltaTime;
        }
        if (Input.GetKey(Silk.NET.SDL.KeyCode.KS))
        {
            OwnerActor.Transform.Position += new Vector2D<float>(0, -100) * Time.DeltaTime;
        }
        if (Input.GetKey(Silk.NET.SDL.KeyCode.KD))
        {
            OwnerActor.Transform.Position += new Vector2D<float>(100, 0) * Time.DeltaTime;
        }

        if (Input.GetKey(Silk.NET.SDL.KeyCode.KR))
        {
            OwnerActor.Transform.Rotation += Time.DeltaTime;
        }
    }
}
