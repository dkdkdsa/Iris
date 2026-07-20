using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace IrisEditor.Workspace
{
    internal sealed class GameBuilder
    {
        private Process _process;
        private List<string> _output;
        private Action<bool> _onDone;

        public bool Building => _process != null;

        public void Build(string projectFile, string outputDirectory, Action<bool> onDone)
        {
            if (_process != null)
                return;

            _onDone = onDone;
            _output = new List<string>();

            var psi = new ProcessStartInfo("dotnet", $"publish \"{projectFile}\" -c Release -o \"{outputDirectory}\" --nologo -v q")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                _process = Process.Start(psi);
                _process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (_output) _output.Add(e.Data); };
                _process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (_output) _output.Add(e.Data); };
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                Console.WriteLine($"[에디터] 게임 빌드 시작: {Path.GetFileName(projectFile)} → {outputDirectory}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[에디터] dotnet 실행 실패: {ex.Message}");
                _process = null;
                _onDone = null;
                onDone?.Invoke(false);
            }
        }

        public void Update()
        {
            if (_process == null || !_process.HasExited)
                return;

            int exitCode = _process.ExitCode;
            _process.Dispose();
            _process = null;

            var onDone = _onDone;
            _onDone = null;

            if (exitCode != 0)
            {
                Console.WriteLine("[에디터] 게임 빌드 실패:");

                lock (_output)
                {
                    foreach (var line in _output)
                        Console.WriteLine("  " + line);
                }

                onDone?.Invoke(false);
                return;
            }

            onDone?.Invoke(true);
        }
    }
}
