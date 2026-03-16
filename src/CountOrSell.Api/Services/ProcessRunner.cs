using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class ProcessRunner : IProcessRunner
{
    public async Task<string> RunAsync(
        string executable,
        string arguments,
        Dictionary<string, string>? environment,
        string? stdinInput,
        CancellationToken ct)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdinInput != null,
            UseShellExecute = false
        };

        if (environment != null)
        {
            foreach (var kv in environment)
                psi.Environment[kv.Key] = kv.Value;
        }

        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {executable}");

        if (stdinInput != null)
        {
            await process.StandardInput.WriteAsync(stdinInput);
            process.StandardInput.Close();
        }

        var output = await process.StandardOutput.ReadToEndAsync(ct);
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{executable} exited with code {process.ExitCode}: {error}");

        return output;
    }
}
