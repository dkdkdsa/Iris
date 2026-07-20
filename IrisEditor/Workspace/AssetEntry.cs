using System;
using System.Collections.Generic;
using System.Text;

namespace IrisEditor.Workspace
{
    internal class AssetEntry
    {
        public Guid Id { get; set; }
        public string Path { get; set; }
        public Type AssetType { get; set; }
    }
}
