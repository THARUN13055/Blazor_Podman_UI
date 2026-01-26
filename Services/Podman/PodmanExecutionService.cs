using System.Diagnostics;
using Blazor.Models;
using Blazor.Models.Podman;


namespace Blazor.Services;

public class PodmanExecutionService
{
    public async Task<PodmanCommandResult> RunAsync(
        string arguments,
        Action<string> onOutput,
        Action<string> onError)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "podman",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = psi };

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                onOutput(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                onError(e.Data);
        };

        process.Start();
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

