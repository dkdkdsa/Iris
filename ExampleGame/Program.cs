using Iris;
using Iris.Assets;
using Iris.Core;
using Iris.Platform;
using System;

namespace ExampleGame
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Engine engine = new Engine(new DefaultPlatform());
            engine.OnStart += HandleStart;
            engine.Run(new WindowConfig
            {
                width = 800,
                height = 600,
                title = "Hello",
            });
        }

        private static void HandleStart()
        {
            var boot = new Bootstrap();
            boot.Start();
        }
    }
}
