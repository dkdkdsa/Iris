using IrisEditor.Data;
using IrisEditor.Serialization;
using System.IO;
using System.Text;

namespace IrisEditor.Workspace
{
    internal static class BuiltinAssetCreators
    {
        [AssetCreator("폴더", "NewFolder")]
        private static void CreateFolder(string path)
        {
            Directory.CreateDirectory(path);
        }

        [AssetCreator("씬", "NewScene.scene")]
        private static void CreateScene(string path)
        {
            SceneSerializer.Save(new SceneData(), path);
        }

        [AssetCreator("C# 스크립트", "NewScript.cs")]
        private static void CreateScript(string path)
        {
            string className = SanitizeClassName(Path.GetFileNameWithoutExtension(path));

            File.WriteAllText(path, $$"""
                using Iris.Core;

                public class {{className}} : Component
                {
                    public override void Update()
                    {
                    }
                }
                """);
        }

        private static string SanitizeClassName(string name)
        {
            var builder = new StringBuilder(name.Length);

            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    builder.Append(c);
            }

            if (builder.Length == 0)
                return "NewScript";

            if (char.IsDigit(builder[0]))
                builder.Insert(0, '_');

            return builder.ToString();
        }
    }
}
