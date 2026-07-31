using Iris.Debugging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using Debug = Iris.Debugging.Debug;

namespace IrisEditor.Platform
{
    [SupportedOSPlatform("windows")]
    internal static class ExternalEditor
    {
        private static string _devenvPath;
        private static bool _searched;

        public static void OpenScript(string projectFile, string filePath)
        {
            string devenv = FindDevenv();

            try
            {
                if (devenv == null)
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    return;
                }

                bool running = Process.GetProcessesByName("devenv").Length > 0;

                string arguments = running || projectFile == null
                    ? $"/edit \"{filePath}\""
                    : $"\"{projectFile}\" \"{filePath}\"";

                Process.Start(new ProcessStartInfo(devenv, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
            }
            catch (Exception ex)
            {
                Debug.LogException("스크립트 열기 실패", ex);
            }
        }

        private static string FindDevenv()
        {
            if (_searched)
                return _devenvPath;

            _searched = true;

            try
            {
                string vswhere = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft Visual Studio", "Installer", "vswhere.exe");

                if (File.Exists(vswhere))
                {
                    var psi = new ProcessStartInfo(vswhere, "-latest -prerelease -products * -property productPath")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };

                    using var process = Process.Start(psi);
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(3000);

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        string first = output.Split('\n')[0].Trim();

                        if (File.Exists(first))
                            _devenvPath = first;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException("Visual Studio 탐색 실패", ex);
            }

            if (_devenvPath == null)
                Debug.LogWarning("Visual Studio를 찾지 못했습니다. OS 기본 프로그램으로 엽니다.");

            return _devenvPath;
        }
    }
}
