using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Iris.Build.Step
{
    public sealed class CompileStep : IBuildStep
    {
        public string Name => "게임 컴파일";

        public async Task<bool> Run(BuildContext context)
        {
            var psi = new ProcessStartInfo("dotnet",
                $"publish \"{context.ProjectFile}\" -c Release -o \"{context.OutputDirectory}\" --nologo -v q")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var output = new List<string>();

            using var process = Process.Start(psi);

            if (process == null)
            {
                context.Log("dotnet을 실행하지 못했습니다.");
                return false;
            }

            process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (output) output.Add(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (output) output.Add(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                lock (output)
                {
                    foreach (var line in output)
                        context.Log("  " + line);
                }

                return false;
            }

            return true;
        }
    }
}
