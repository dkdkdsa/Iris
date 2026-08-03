using Iris.Debugging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;

namespace IrisEditor.Workspace
{
    internal static class AssemblyDefinitions
    {
        public const string Extension = ".asmdef";
        public const string GeneratedProjectSuffix = ".g.csproj";

        private static readonly HashSet<string> _ignoredDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".vs", "bin", "obj",
        };

        public static List<AssemblyDefinition> Scan(string rootPath)
        {
            var result = new List<AssemblyDefinition>();

            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                return result;

            Collect(rootPath, rootPath, result);
            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            return result;
        }

        private static void Collect(string rootPath, string directory, List<AssemblyDefinition> result)
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*" + Extension))
            {
                var definition = Read(rootPath, file);

                if (definition != null)
                    result.Add(definition);
            }

            foreach (var sub in Directory.EnumerateDirectories(directory))
            {
                if (_ignoredDirectories.Contains(Path.GetFileName(sub)))
                    continue;

                Collect(rootPath, sub, result);
            }
        }

        private static AssemblyDefinition Read(string rootPath, string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);

            var definition = new AssemblyDefinition
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath,
                DirectoryPath = directory,
                RelativeDirectory = Path.GetRelativePath(rootPath, directory).Replace('\\', '/'),
            };

            if (definition.RelativeDirectory == ".")
                definition.RelativeDirectory = string.Empty;

            try
            {
                if (JsonNode.Parse(File.ReadAllText(filePath)) is not JsonObject root)
                    return definition;

                if (root["name"] is JsonValue nameValue && nameValue.TryGetValue(out string name) &&
                    !string.IsNullOrWhiteSpace(name))
                    definition.Name = Sanitize(name);

                if (root["allowUnsafeCode"] is JsonValue unsafeValue && unsafeValue.TryGetValue(out bool allowUnsafe))
                    definition.AllowUnsafeCode = allowUnsafe;

                if (root["references"] is JsonArray references)
                {
                    foreach (var node in references)
                    {
                        if (node is JsonValue value && value.TryGetValue(out string reference) &&
                            !string.IsNullOrWhiteSpace(reference))
                            definition.References.Add(Sanitize(reference));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException($"Failed to read assembly definition: {Path.GetFileName(filePath)}", ex);
            }

            return definition;
        }

        public static List<AssemblyDefinition> Generate(string rootPath)
        {
            var definitions = Scan(rootPath);
            var byName = new Dictionary<string, AssemblyDefinition>(StringComparer.OrdinalIgnoreCase);
            var accepted = new List<AssemblyDefinition>();

            foreach (var definition in definitions)
            {
                if (string.IsNullOrWhiteSpace(definition.Name))
                {
                    Debug.LogWarning($"Assembly definition has no usable name: {definition.FilePath}");
                    continue;
                }

                if (byName.TryGetValue(definition.Name, out var existing))
                {
                    Debug.LogWarning($"Duplicate assembly name '{definition.Name}'; ignoring {definition.FilePath} (already defined by {existing.FilePath})");
                    continue;
                }

                if (definition.RelativeDirectory.Length == 0)
                {
                    Debug.LogWarning($"Assembly definition cannot sit in the project root: {definition.FilePath}");
                    continue;
                }

                byName[definition.Name] = definition;
                accepted.Add(definition);
            }

            RemoveStaleProjects(rootPath, accepted);

            foreach (var definition in accepted)
                WriteProject(rootPath, definition, accepted, byName);

            return accepted;
        }

        private static void RemoveStaleProjects(string rootPath, List<AssemblyDefinition> accepted)
        {
            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var definition in accepted)
                expected.Add(Path.GetFullPath(definition.ProjectPath));

            foreach (var file in Directory.EnumerateFiles(rootPath, "*" + GeneratedProjectSuffix, SearchOption.AllDirectories))
            {
                if (expected.Contains(Path.GetFullPath(file)))
                    continue;

                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Debug.LogException($"Failed to remove stale project: {Path.GetFileName(file)}", ex);
                }
            }
        }

        private static void WriteProject(string rootPath, AssemblyDefinition definition,
            List<AssemblyDefinition> all, Dictionary<string, AssemblyDefinition> byName)
        {
            var builder = new StringBuilder();

            builder.AppendLine("<!--");
            builder.AppendLine("  Generated from the assembly definition next to this file.");
            builder.AppendLine("  Manual edits are lost the next time the project is opened.");
            builder.AppendLine("-->");
            builder.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            builder.AppendLine();
            builder.AppendLine("  <PropertyGroup>");
            builder.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
            builder.AppendLine("    <OutputType>Library</OutputType>");
            builder.AppendLine($"    <AssemblyName>{definition.Name}</AssemblyName>");
            builder.AppendLine($"    <RootNamespace>{definition.Name}</RootNamespace>");
            builder.AppendLine("    <ImplicitUsings>disable</ImplicitUsings>");
            builder.AppendLine("    <Nullable>disable</Nullable>");
            builder.AppendLine($"    <AllowUnsafeBlocks>{(definition.AllowUnsafeCode ? "true" : "false")}</AllowUnsafeBlocks>");
            builder.AppendLine("  </PropertyGroup>");
            builder.AppendLine();

            foreach (var nested in NestedDirectories(definition, all))
            {
                builder.AppendLine("  <ItemGroup>");
                builder.AppendLine($"    <Compile Remove=\"{nested}\\**\\*.cs\" />");
                builder.AppendLine("  </ItemGroup>");
            }

            string enginePropsPath = Path.GetRelativePath(definition.DirectoryPath,
                Path.Combine(rootPath, ProjectScaffolder.EngineOnlyPropsFileName)).Replace('/', '\\');

            builder.AppendLine($"  <Import Project=\"{enginePropsPath}\" Condition=\"Exists('{enginePropsPath}')\" />");

            var projectReferences = new List<string>();

            foreach (var reference in definition.References)
            {
                if (!byName.TryGetValue(reference, out var target))
                {
                    Debug.LogWarning($"Assembly '{definition.Name}' references unknown assembly '{reference}'");
                    continue;
                }

                if (target == definition)
                    continue;

                projectReferences.Add(Path.GetRelativePath(definition.DirectoryPath, target.ProjectPath).Replace('/', '\\'));
            }

            if (projectReferences.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("  <ItemGroup>");

                foreach (var reference in projectReferences)
                    builder.AppendLine($"    <ProjectReference Include=\"{reference}\" />");

                builder.AppendLine("  </ItemGroup>");
            }

            builder.AppendLine();
            builder.AppendLine("</Project>");

            try
            {
                string content = builder.ToString();

                if (!File.Exists(definition.ProjectPath) ||
                    !string.Equals(File.ReadAllText(definition.ProjectPath), content, StringComparison.Ordinal))
                    File.WriteAllText(definition.ProjectPath, content);
            }
            catch (Exception ex)
            {
                Debug.LogException($"Failed to write project for '{definition.Name}'", ex);
            }
        }

        public static List<string> NestedDirectories(AssemblyDefinition definition, List<AssemblyDefinition> all)
        {
            var result = new List<string>();
            string prefix = definition.RelativeDirectory.Length == 0
                ? string.Empty
                : definition.RelativeDirectory + "/";

            foreach (var other in all)
            {
                if (other == definition || other.RelativeDirectory.Length == 0)
                    continue;

                if (prefix.Length > 0 && !other.RelativeDirectory.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string rest = other.RelativeDirectory.Substring(prefix.Length);

                if (rest.Length == 0)
                    continue;

                result.Add(rest.Replace('/', '\\'));
            }

            return result;
        }

        private static string Sanitize(string value)
        {
            var builder = new StringBuilder();

            foreach (char c in value.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '.' || c == '_')
                    builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
