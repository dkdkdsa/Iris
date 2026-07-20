using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace IrisEditor.Workspace
{
    internal sealed class AssetCreator
    {
        public string MenuName { get; init; }
        public string DefaultFileName { get; init; }
        public Action<string> Create { get; init; }
    }

    internal static class AssetCreatorRegistry
    {
        public static IReadOnlyList<AssetCreator> Creators { get; } = Scan();

        public static string MakeUniquePath(string directory, string defaultFileName)
        {
            string name = Path.GetFileNameWithoutExtension(defaultFileName);
            string extension = Path.GetExtension(defaultFileName);
            string path = Path.Combine(directory, defaultFileName);
            int index = 1;

            while (File.Exists(path) || Directory.Exists(path))
                path = Path.Combine(directory, $"{name} ({index++}){extension}");

            return path;
        }

        private static List<AssetCreator> Scan()
        {
            var result = new List<AssetCreator>();

            foreach (var type in typeof(AssetCreatorRegistry).Assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attr = method.GetCustomAttribute<AssetCreatorAttribute>();
                    if (attr == null)
                        continue;

                    var parameters = method.GetParameters();

                    if (method.ReturnType != typeof(void) || parameters.Length != 1 ||
                        parameters[0].ParameterType != typeof(string))
                    {
                        Console.WriteLine($"[에디터] AssetCreator 시그니처가 잘못되어 무시함: {type.Name}.{method.Name} — static void (string path) 여야 함");
                        continue;
                    }

                    result.Add(new AssetCreator
                    {
                        MenuName = attr.MenuName,
                        DefaultFileName = attr.DefaultFileName,
                        Create = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), method),
                    });
                }
            }

            result.Sort((a, b) => string.Compare(a.MenuName, b.MenuName, StringComparison.OrdinalIgnoreCase));
            return result;
        }
    }
}
