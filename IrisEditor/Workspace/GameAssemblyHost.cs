using Iris.Debugging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Text;
using Debug = Iris.Debugging.Debug;

namespace IrisEditor.Workspace
{
    internal sealed class GameAssemblyHost
    {
        private sealed class GameLoadContext : AssemblyLoadContext
        {
            private readonly string _directory;

            public GameLoadContext(string directory) : base(isCollectible: true)
            {
                _directory = directory;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                if (_directory == null || assemblyName.Name == null)
                    return null;

                string candidate = Path.Combine(_directory, assemblyName.Name + ".dll");

                if (!File.Exists(candidate) || !ReferencesEngine(candidate))
                    return null;

                using var stream = new MemoryStream(File.ReadAllBytes(candidate));
                return LoadFromStream(stream);
            }
        }

        private IReadOnlyList<Assembly> _assemblies = Array.Empty<Assembly>();
        private GameLoadContext _loadContext;
        private Process _buildProcess;
        private List<string> _buildOutput;
        private string _projectFile;
        private Action<bool> _onDone;

        public Assembly Current { get; private set; }

        public IReadOnlyList<Assembly> Assemblies => _assemblies;

        public bool Building => _buildProcess != null;

        public void BuildAndLoad(string projectFile, Action<bool> onDone)
        {
            if (_buildProcess != null)
                return;

            _projectFile = projectFile;
            _onDone = onDone;
            _buildOutput = new List<string>();

            var psi = new ProcessStartInfo("dotnet", $"build \"{projectFile}\" --nologo -v q")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                _buildProcess = Process.Start(psi);
                _buildProcess.OutputDataReceived += (_, e) => { if (e.Data != null) lock (_buildOutput) _buildOutput.Add(e.Data); };
                _buildProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (_buildOutput) _buildOutput.Add(e.Data); };
                _buildProcess.BeginOutputReadLine();
                _buildProcess.BeginErrorReadLine();

                Debug.Log($"Script build started: {Path.GetFileName(projectFile)}");
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to run dotnet", ex);
                _buildProcess = null;
                _onDone = null;
                onDone?.Invoke(false);
            }
        }

        public void Update()
        {
            if (_buildProcess == null || !_buildProcess.HasExited)
                return;

            int exitCode = _buildProcess.ExitCode;
            _buildProcess.Dispose();
            _buildProcess = null;

            var onDone = _onDone;
            _onDone = null;

            if (exitCode != 0)
            {
                var builder = new StringBuilder("Script build failed:");

                lock (_buildOutput)
                {
                    foreach (var line in _buildOutput)
                    {
                        builder.AppendLine();
                        builder.Append("  ");
                        builder.Append(line);
                    }
                }

                Debug.LogError(builder.ToString());

                onDone?.Invoke(false);
                return;
            }

            onDone?.Invoke(TryLoad());
        }

        private bool TryLoad()
        {
            string dll = FindOutputDll();

            if (dll == null)
            {
                Debug.LogError("Build output DLL not found.");
                return false;
            }

            try
            {
                _loadContext?.Unload();
                _loadContext = new GameLoadContext(Path.GetDirectoryName(dll));

                var loaded = new List<Assembly> { LoadFrom(dll) };

                foreach (var sibling in FindGameAssemblies(Path.GetDirectoryName(dll), dll))
                {
                    var assembly = LoadFrom(sibling);

                    if (assembly != null)
                        loaded.Add(assembly);
                }

                Current = loaded[0];
                _assemblies = loaded;

                Debug.Log(loaded.Count == 1
                    ? $"Game assembly loaded: {Path.GetFileName(dll)}"
                    : $"Game assemblies loaded: {string.Join(", ", loaded.ConvertAll(a => a.GetName().Name))}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to load assembly", ex);
                Current = null;
                _assemblies = Array.Empty<Assembly>();
                return false;
            }
        }

        private Assembly LoadFrom(string path)
        {
            using var stream = new MemoryStream(File.ReadAllBytes(path));
            return _loadContext.LoadFromStream(stream);
        }

        private static List<string> FindGameAssemblies(string directory, string rootDll)
        {
            var result = new List<string>();
            string rootName = Path.GetFileName(rootDll);

            foreach (var file in Directory.GetFiles(directory, "*.dll"))
            {
                if (string.Equals(Path.GetFileName(file), rootName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (ReferencesEngine(file))
                    result.Add(file);
            }

            return result;
        }

        private static bool ReferencesEngine(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var reader = new PEReader(stream);

                if (!reader.HasMetadata)
                    return false;

                var metadata = reader.GetMetadataReader();

                foreach (var handle in metadata.AssemblyReferences)
                {
                    if (metadata.GetString(metadata.GetAssemblyReference(handle).Name) == "Iris")
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private string FindOutputDll()
        {
            string directory = Path.GetDirectoryName(_projectFile);
            string name = Path.GetFileNameWithoutExtension(_projectFile);
            string bin = Path.Combine(directory, "bin");

            if (!Directory.Exists(bin))
                return null;

            string best = null;
            var bestTime = DateTime.MinValue;

            foreach (var file in Directory.GetFiles(bin, name + ".dll", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}"))
                    continue;

                var time = File.GetLastWriteTimeUtc(file);

                if (time > bestTime)
                {
                    bestTime = time;
                    best = file;
                }
            }

            return best;
        }
    }
}
