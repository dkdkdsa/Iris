using Iris;
using Iris.Assets;
using Iris.Core;
using Iris.Platform;
using IrisEditor.Localization;
using System;
using System.IO;
using Debug = Iris.Debugging.Debug;

namespace IrisEditor
{
    internal static class Program
    {
        static void Main()
        {
            Debug.DefaultChannel = "Editor";

            EditorSettings.Load();
            Loc.Load(Path.Combine(AppContext.BaseDirectory, "Localization"));
            Loc.SetLanguage(EditorSettings.Language);
            Loc.EnableHotReload();

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

            host.Platform.FileDropped += context.QueueDroppedPath;

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
