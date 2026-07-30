using Iris.Core;
using IrisEditor.Data;
using IrisEditor.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IrisEditor.Workspace
{
    internal static class ProjectScaffolder
    {
        private static readonly string[] _nativeDlls = { "SDL2.dll", "cimgui.dll", "stbi.dll" };

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

            string csproj = engineProject != null
                ? SourceReferenceCsproj(engineProject)
                : ResolveEngineCsproj(out error);

            if (csproj == null)
                return false;

            try
            {
                Directory.CreateDirectory(projectRoot);
                Directory.CreateDirectory(Path.Combine(projectRoot, "Scenes"));
                Directory.CreateDirectory(Path.Combine(projectRoot, "Resources"));

                File.WriteAllText(Path.Combine(projectRoot, $"{name}.csproj"), csproj);
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

        private static string ResolveEngineCsproj(out string error)
        {
            error = null;

            string sourceProject = FindEngineProject();

            if (sourceProject != null)
                return SourceReferenceCsproj(sourceProject);

            string engineDll = Path.Combine(AppContext.BaseDirectory, "Iris.dll");

            if (File.Exists(engineDll))
                return DllReferenceCsproj(engineDll);

            error = "엔진(Iris.csproj 또는 Iris.dll)을 찾을 수 없습니다.";
            return null;
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

        private static List<string> FindNativeDlls(string engineDir)
        {
            var result = new List<string>();

            foreach (var native in _nativeDlls)
            {
                string ridPath = Path.Combine(engineDir, "runtimes", "win-x64", "native", native);
                string rootPath = Path.Combine(engineDir, native);

                if (File.Exists(ridPath))
                    result.Add(ridPath);
                else if (File.Exists(rootPath))
                    result.Add(rootPath);
            }

            return result;
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

        private static string SourceReferenceCsproj(string engineProject)
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

                {ContentItemGroup()}
                </Project>
                """;
        }

        private static string DllReferenceCsproj(string engineDll)
        {
            string engineDir = Path.GetDirectoryName(engineDll);

            var natives = new StringBuilder();

            foreach (var native in FindNativeDlls(engineDir))
                natives.AppendLine($"""    <None Include="{native}" Link="{Path.GetFileName(native)}" CopyToOutputDirectory="PreserveNewest" />""");

            return $"""
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>disable</ImplicitUsings>
                    <Nullable>disable</Nullable>
                  </PropertyGroup>

                  <ItemGroup>
                    <Reference Include="Iris">
                      <HintPath>{engineDll}</HintPath>
                    </Reference>
                  </ItemGroup>

                  <ItemGroup>
                {natives.ToString().TrimEnd()}
                  </ItemGroup>

                {ContentItemGroup()}
                </Project>
                """;
        }

        private static string ContentItemGroup()
        {
            return """
                  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
                    <Content Include="**\*.png;**\*.jpg;**\*.jpeg;**\*.bmp;**\*.tga;**\*.gif;**\*.wav;**\*.mp3;**\*.ttf;**\*.otf;**\*.ui;**\*.anim;**\*.sprite;**\*.tile;**\*.controller;**\*.prefab;**\*.scene;project.json" Exclude="bin\**;obj\**" CopyToOutputDirectory="PreserveNewest" />
                  </ItemGroup>
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
                  "buildScenes": [ "Scenes/Main.scene" ],
                  "width": 1280,
                  "height": 720,
                  "title": "{{name}}",
                  "fullscreen": false,
                  "resizable": false
                }
                """;
        }
    }
}
