using Iris.Core;
using IrisEditor.Data;
using IrisEditor.Serialization;
using System;
using System.IO;

namespace IrisEditor.Workspace
{
    internal static class ProjectScaffolder
    {
        public static bool TryCreate(string projectRoot, out string error, string engineProject = null)
        {
            error = null;

            string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(projectRoot));

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "프로젝트 폴더 이름이 올바르지 않습니다.";
                return false;
            }

            if (Directory.Exists(projectRoot) &&
                (Directory.GetFiles(projectRoot).Length > 0 || Directory.GetDirectories(projectRoot).Length > 0))
            {
                error = $"폴더가 비어있지 않습니다: {projectRoot}";
                return false;
            }

            engineProject ??= FindEngineProject();

            if (engineProject == null)
            {
                error = "엔진 프로젝트(Iris.csproj)를 찾을 수 없습니다.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(projectRoot);
                Directory.CreateDirectory(Path.Combine(projectRoot, "Scenes"));
                Directory.CreateDirectory(Path.Combine(projectRoot, "Resources"));

                File.WriteAllText(Path.Combine(projectRoot, $"{name}.csproj"), CsprojTemplate(engineProject));
                File.WriteAllText(Path.Combine(projectRoot, "Program.cs"), ProgramTemplate());
                File.WriteAllText(Path.Combine(projectRoot, "project.json"), ProjectJsonTemplate(name));

                SceneSerializer.Save(DefaultScene(), Path.Combine(projectRoot, "Scenes", "Main.scene"));

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string FindEngineProject()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "Iris", "Iris.csproj");

                if (File.Exists(candidate))
                    return candidate;

                dir = dir.Parent;
            }

            return null;
        }

        private static SceneData DefaultScene()
        {
            var scene = new SceneData();

            var camera = new ActorData
            {
                Id = Guid.NewGuid(),
                Name = "Main Camera",
            };

            camera.Components.Add(new ComponentData
            {
                Id = Guid.NewGuid(),
                TargetType = typeof(Transform),
                Properties = ComponentCatalog.DefaultProperties(typeof(Transform)),
            });

            camera.Components.Add(new ComponentData
            {
                Id = Guid.NewGuid(),
                TargetType = typeof(Camera),
                Properties = ComponentCatalog.DefaultProperties(typeof(Camera)),
            });

            scene.Actors.Add(camera);
            return scene;
        }

        private static string CsprojTemplate(string engineProject)
        {
            return $"""
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>disable</ImplicitUsings>
                    <Nullable>disable</Nullable>
                  </PropertyGroup>

                  <ItemGroup>
                    <ProjectReference Include="{engineProject}" />
                  </ItemGroup>

                  <ItemGroup>
                    <Content Include="**\*.png;**\*.jpg;**\*.jpeg;**\*.bmp;**\*.tga;**\*.gif;**\*.wav;**\*.mp3;**\*.scene;project.json" Exclude="bin\**;obj\**" CopyToOutputDirectory="PreserveNewest" />
                  </ItemGroup>

                </Project>
                """;
        }

        private static string ProgramTemplate()
        {
            return """
                using Iris;

                GameBootstrap.Run(args);
                """;
        }

        private static string ProjectJsonTemplate(string name)
        {
            return $$"""
                {
                  "startScene": "Scenes/Main.scene",
                  "width": 1280,
                  "height": 720,
                  "title": "{{name}}"
                }
                """;
        }
    }
}
