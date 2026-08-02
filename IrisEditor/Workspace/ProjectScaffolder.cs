using Iris.Core;
using IrisEditor.Data;
using IrisEditor.Serialization;
using System;
using System.IO;
using System.Text;

namespace IrisEditor.Workspace
{
    internal static class ProjectScaffolder
    {
        public const string EnginePropsFileName = "Iris.g.props";

        private const string ReleaseWindowGroup = """
              <PropertyGroup Condition="'$(Configuration)' == 'Release' And '$(IrisConsoleWindow)' != 'true'">
                <OutputType>WinExe</OutputType>
              </PropertyGroup>
            """;

        private static readonly string[] _nativeDlls = { "SDL2.dll", "cimgui.dll", "stbi.dll" };
        private static readonly string[] _editorOnlyDlls = { "IrisEditor.dll", "Iris.Build.dll" };

        public static bool TryCreate(string projectRoot, out string error, string engineProject = null)
        {
            error = null;

            string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(projectRoot));

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "The project folder name is not valid.";
                return false;
            }

            if (Directory.Exists(projectRoot) &&
                (Directory.GetFiles(projectRoot).Length > 0 || Directory.GetDirectories(projectRoot).Length > 0))
            {
                error = $"The folder is not empty: {projectRoot}";
                return false;
            }

            string props = ResolveEngineProps(engineProject, out error);

            if (props == null)
                return false;

            try
            {
                Directory.CreateDirectory(projectRoot);
                Directory.CreateDirectory(Path.Combine(projectRoot, "Scenes"));
                Directory.CreateDirectory(Path.Combine(projectRoot, "Resources"));

                File.WriteAllText(Path.Combine(projectRoot, $"{name}.csproj"), CsprojTemplate());
                File.WriteAllText(Path.Combine(projectRoot, EnginePropsFileName), props);
                File.WriteAllText(Path.Combine(projectRoot, ".gitignore"), GitIgnoreTemplate());
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

        public static bool TryWriteEngineReferences(string projectRoot, out string error, string engineProject = null)
        {
            string props = ResolveEngineProps(engineProject, out error);

            if (props == null)
                return false;

            try
            {
                string path = Path.Combine(projectRoot, EnginePropsFileName);

                if (MatchesOnDisk(path, props))
                    return true;

                File.WriteAllText(path, props);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool MatchesOnDisk(string path, string content)
        {
            try
            {
                return File.Exists(path) && string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static string ResolveEngineProps(string engineProject, out string error)
        {
            error = null;

            if (engineProject != null)
                return SourceReferenceProps(engineProject);

            string sourceProject = FindEngineProject();

            if (sourceProject != null)
                return SourceReferenceProps(sourceProject);

            string engineDir = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);

            if (File.Exists(Path.Combine(engineDir, "Iris.dll")))
                return DllReferenceProps(engineDir);

            error = "Engine not found (Iris.csproj or Iris.dll).";
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

        private static string CsprojTemplate()
        {
            return $"""
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>disable</ImplicitUsings>
                    <Nullable>disable</Nullable>
                  </PropertyGroup>

                  <Import Project="{EnginePropsFileName}" Condition="Exists('{EnginePropsFileName}')" />

                {ContentItemGroup()}
                </Project>
                """;
        }

        private static string SourceReferenceProps(string engineProject)
        {
            return $"""
                <Project>

                {ReleaseWindowGroup}

                  <ItemGroup>
                    <ProjectReference Include="{engineProject}" />
                  </ItemGroup>

                </Project>
                """;
        }

        private static string DllReferenceProps(string engineDir)
        {
            var excludes = new StringBuilder();

            foreach (var excluded in _editorOnlyDlls)
                AppendExclude(excludes, excluded);

            foreach (var excluded in _nativeDlls)
                AppendExclude(excludes, excluded);

            return $"""
                <Project>

                {ReleaseWindowGroup}

                  <PropertyGroup>
                    <IrisEngineDir>{engineDir}</IrisEngineDir>
                  </PropertyGroup>

                  <ItemGroup>
                    <Reference Include="$(IrisEngineDir)\*.dll" Exclude="{excludes}" />
                  </ItemGroup>

                  <ItemGroup>
                {NativeItems(engineDir)}  </ItemGroup>

                </Project>
                """;
        }

        private static void AppendExclude(StringBuilder builder, string fileName)
        {
            if (builder.Length > 0)
                builder.Append(';');

            builder.Append("$(IrisEngineDir)\\").Append(fileName);
        }

        private static string NativeItems(string engineDir)
        {
            var builder = new StringBuilder();
            string nativeDir = Path.Combine(engineDir, "runtimes", "win-x64", "native");

            if (Directory.Exists(nativeDir))
            {
                builder.AppendLine("""    <None Include="$(IrisEngineDir)\runtimes\win-x64\native\*.dll" Link="%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />""");
                return builder.ToString();
            }

            foreach (var native in _nativeDlls)
            {
                if (File.Exists(Path.Combine(engineDir, native)))
                    builder.AppendLine($"""    <None Include="$(IrisEngineDir)\{native}" Link="{native}" CopyToOutputDirectory="PreserveNewest" />""");
            }

            return builder.ToString();
        }

        private static string ContentItemGroup()
        {
            return """
                  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
                    <Content Include="**\*.png;**\*.jpg;**\*.jpeg;**\*.bmp;**\*.tga;**\*.gif;**\*.wav;**\*.mp3;**\*.ttf;**\*.otf;**\*.ui;**\*.anim;**\*.sprite;**\*.tile;**\*.controller;**\*.prefab;**\*.scene;project.json" Exclude="bin\**;obj\**" CopyToOutputDirectory="PreserveNewest" />
                  </ItemGroup>
                """;
        }

        private const string VisualStudioIgnore = """
            ## Build results
            [Dd]ebug/
            [Dd]ebugPublic/
            [Rr]elease/
            [Rr]eleases/
            x64/
            x86/
            [Ww][Ii][Nn]32/
            [Aa][Rr][Mm]/
            [Aa][Rr][Mm]64/
            bld/
            [Bb]in/
            [Oo]bj/
            [Oo]ut/
            [Ll]og/
            [Ll]ogs/
            artifacts/

            ## User-specific files
            *.rsuser
            *.suo
            *.user
            *.userosscache
            *.sln.docstates
            *.userprefs

            ## Visual Studio cache and options
            .vs/
            *.vspscc
            *.vssscc
            *.pidb
            *.svclog
            *.scc
            Generated\ Files/

            ## Intermediate and temporary files
            *.ilk
            *.meta
            *.obj
            *.iobj
            *.pch
            *.pdb
            *.ipdb
            *.pgc
            *.pgd
            *.rsp
            *.sbr
            *.tlb
            *.tli
            *.tlh
            *.tmp
            *.tmp_proj
            *_wpftmp.csproj
            *.log
            *.binlog
            *.[Cc]ache
            !?*.[Cc]ache/
            ~$*
            *~
            *.dbmdl
            *.jfm
            mono_crash.*

            ## Test results and coverage
            [Tt]est[Rr]esult*/
            [Bb]uild[Ll]og.*
            *.VisualState.xml
            TestResult.xml
            nunit-*.xml
            *.coverage
            *.coveragexml
            coverage*.json
            coverage*.xml
            coverage*.info
            BenchmarkDotNet.Artifacts/

            ## NuGet
            *.nupkg
            *.snupkg
            **/[Pp]ackages/*
            !**/[Pp]ackages/build/
            project.lock.json
            project.fragment.lock.json
            *.nuget.props
            *.nuget.targets

            ## Publish output
            publish/
            PublishScripts/
            *.[Pp]ublish.xml
            *.azurePubxml
            *.pubxml
            *.publishproj
            *.publishsettings
            *.pfx

            ## ReSharper / Rider
            .idea/
            _ReSharper*/
            *.[Rr]e[Ss]harper
            *.DotSettings.user

            ## Other tooling
            _TeamCity*
            *.dotCover
            _NCrunch_*
            .*crunch*.local.xml
            nCrunchTemp_*
            .localhistory/
            .ionide/

            ## OS junk
            Thumbs.db
            ehthumbs.db
            Desktop.ini
            .DS_Store
            """;

        private static string GitIgnoreTemplate()
        {
            return $"""
                {VisualStudioIgnore}

                ## Iris
                # Regenerated by the editor; contains machine-specific engine paths
                {EnginePropsFileName}

                # Dear ImGui window layout, written next to the running game
                imgui.ini
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
                  "resizable": false,
                  "vsync": true,
                  "targetFrameRate": 0,
                  "logToFile": true,
                  "stats": false,
                  "batchByTexture": false
                }
                """;
        }
    }
}
