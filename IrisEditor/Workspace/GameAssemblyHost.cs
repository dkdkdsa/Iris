using Iris.Debugging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace IrisEditor.Workspace
{
    internal sealed class GameAssemblyHost
    {
        private static readonly DebugChannel _log = Iris.Debugging.Debug.Channel("Editor");

        private sealed class GameLoadContext : AssemblyLoadContext
        {
            public GameLoadContext() : base(isCollectible: true)
            {
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                return null;
            }
        }

        private GameLoadContext _loadContext;
        private Process _buildProcess;
        private List<string> _buildOutput;
        private string _projectFile;
        private Action<bool> _onDone;

        public Assembly Current { get; private set; }

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

                _log.Log($"스크립트 빌드 시작: {Path.GetFileName(projectFile)}");
            }
            catch (Exception ex)
            {
                _log.LogException("dotnet 실행 실패", ex);
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
                var builder = new StringBuilder("스크립트 빌드 실패:");

                lock (_buildOutput)
                {
                    foreach (var line in _buildOutput)
                    {
                        builder.AppendLine();
                        builder.Append("  ");
                        builder.Append(line);
                    }
                }

                _log.LogError(builder.ToString());

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
                _log.LogError("빌드 산출물 DLL을 찾을 수 없습니다.");
                return false;
            }

            try
            {
                _loadContext?.Unload();
                _loadContext = new GameLoadContext();

                using var stream = new MemoryStream(File.ReadAllBytes(dll));
                Current = _loadContext.LoadFromStream(stream);

                _log.Log($"게임 어셈블리 로드 완료: {Path.GetFileName(dll)}");
                return true;
            }
            catch (Exception ex)
            {
                _log.LogException("어셈블리 로드 실패", ex);
                Current = null;
                return false;
            }
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
