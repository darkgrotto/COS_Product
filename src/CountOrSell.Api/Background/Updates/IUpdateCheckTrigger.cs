namespace CountOrSell.Api.Background.Updates;

public interface IUpdateCheckTrigger
{
    Task<UpdateCheckResult> TriggerAsync(CancellationToken ct);
    Task<UpdateCheckResult> TriggerForceAsync(CancellationToken ct);
}

public record UpdateCheckResult(bool PackagesAvailable, string Message);
