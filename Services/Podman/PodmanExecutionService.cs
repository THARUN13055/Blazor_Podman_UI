using System.Diagnostics;
using Blazor.Models.Podman;

namespace Blazor.Services;

public class PodmanExecutionService
{
    public async Task<PodmanCommandResult> RunAsync(
        string arguments,
        Action<string> onStdout,
        Action<string> onStderr,
        string? stdin = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "podman",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin != null,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                onStdout(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                onStderr(e.Data);
        };

        process.Start();

        if (stdin != null)
        {
            await process.StandardInput.WriteLineAsync(stdin);
            process.StandardInput.Close();
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new PodmanCommandResult
        {
            ExitCode = process.ExitCode,
            Success = process.ExitCode == 0
        };
    }
}
