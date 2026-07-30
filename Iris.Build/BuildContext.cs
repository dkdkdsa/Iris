using Iris.Debugging;
using System;
using System.Collections.Generic;

namespace Iris.Build
{
    public sealed class BuildContext
    {
        private static readonly DebugChannel _log = Debug.Channel("Build");

        public string ProjectFile { get; set; }
        public string ContentRoot { get; set; }
        public string OutputDirectory { get; set; }
        public List<ContentFile> Content { get; } = new();
        public Action<string> Log { get; set; } = message => _log.Log(message);
    }

    public readonly struct ContentFile
    {
        public readonly string Key;
        public readonly string FilePath;

        public ContentFile(string key, string filePath)
        {
            Key = key;
            FilePath = filePath;
        }
    }
}
