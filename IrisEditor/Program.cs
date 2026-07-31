using Iris;
using Iris.Assets;
using Iris.Core;
using Iris.Platform;
using Debug = Iris.Debugging.Debug;

namespace IrisEditor
{
    internal static class Program
    {
        static void Main()
        {
            Debug.DefaultChannel = "Editor";

            var host = new AppHost(new DefaultPlatform());

            host.Initialize(new WindowConfig
            {
                width = 1280,
                height = 720,
                title = "Iris Editor",
                resizable = true,
                vsync = true,
                targetFrameRate = 60,
            });

            AssetManager.Initialize();

            var context = new EditorContext();
            var editor = new EditorApp(context);

            while (!host.IsCloseRequested)
            {
                if (!host.BeginFrame())
                    break;

                editor.Draw();
                host.Present();
            }

            host.Dispose();
        }
    }
}
