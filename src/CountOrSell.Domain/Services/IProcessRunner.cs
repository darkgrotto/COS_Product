namespace CountOrSell.Domain.Services;

public interface IProcessRunner
{
    Task<string> RunAsync(
        string executable,
        string arguments,
        Dictionary<string, string>? environment,
        string? stdinInput,
        CancellationToken ct);
}
