using Iris;
using Iris.Core;
using Iris.Platform;

namespace IrisEditor
{
    internal static class Program
    {
        static void Main()
        {
            var host = new AppHost(new DefaultPlatform());

            host.Initialize(new WindowConfig
            {
                width = 1280,
                height = 720,
                title = "Iris Editor"
            });

            var editor = new EditorApp();

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
