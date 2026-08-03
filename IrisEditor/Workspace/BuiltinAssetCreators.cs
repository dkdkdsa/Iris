using IrisEditor.Data;
using IrisEditor.Serialization;
using System.IO;
using System.Text;
using IrisEditor.Localization;

namespace IrisEditor.Workspace
{
    internal static class BuiltinAssetCreators
    {
        [AssetCreator("asset.folder", "NewFolder")]
        private static void CreateFolder(string path)
        {
            Directory.CreateDirectory(path);
        }

        [AssetCreator("asset.scene", "NewScene.scene")]
        private static void CreateScene(string path)
        {
            SceneSerializer.Save(new SceneData(), path);
        }

        [AssetCreator("asset.uiLayout", "NewLayout.ui")]
        private static void CreateUILayout(string path)
        {
            File.WriteAllText(path, """
                {
                  "uiObjects": []
                }
                """);
        }

        [AssetCreator("asset.animatorController", "NewAnimator.controller")]
        private static void CreateAnimatorController(string path)
        {
            File.WriteAllText(path, """
                {
                  "defaultState": "",
                  "parameters": [],
                  "states": [],
                  "anyTransitions": []
                }
                """);
        }

        [AssetCreator("asset.spriteAnimation", "NewAnimation.anim")]
        private static void CreateAnimationClip(string path)
        {
            File.WriteAllText(path, """
                {
                  "sampleRate": 12,
                  "loop": true,
                  "tracks": [
                    {
                      "component": "Iris.Core.SpriteRenderer",
                      "property": "Sprite",
                      "type": "sprite",
                      "keys": []
                    }
                  ]
                }
                """);
        }

        [AssetCreator("asset.sprite", "NewSprite.sprite")]
        private static void CreateSprite(string path)
        {
            File.WriteAllText(path, """
                {
                  "texture": "",
                  "x": 0,
                  "y": 0,
                  "width": 0,
                  "height": 0
                }
                """);
        }

        [AssetCreator("asset.tile", "NewTile.tile")]
        private static void CreateTile(string path)
        {
            File.WriteAllText(path, """
                {
                  "texture": "",
                  "x": 0,
                  "y": 0,
                  "width": 0,
                  "height": 0
                }
                """);
        }

        [AssetCreator("asset.script", "NewScript.cs")]
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
