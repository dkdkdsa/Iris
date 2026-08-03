using System.Collections.Generic;
using System.IO;

namespace IrisEditor.Workspace
{
    internal sealed class AssemblyDefinition
    {
        public string Name { get; set; }

        public string FilePath { get; set; }

        public string DirectoryPath { get; set; }

        public string RelativeDirectory { get; set; }

        public bool AllowUnsafeCode { get; set; }

        public List<string> References { get; } = new();

        public string ProjectPath => Path.Combine(DirectoryPath, Name + AssemblyDefinitions.GeneratedProjectSuffix);
    }
}
