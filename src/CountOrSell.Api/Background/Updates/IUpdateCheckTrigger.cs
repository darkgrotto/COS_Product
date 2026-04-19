namespace CountOrSell.Api.Background.Updates;

public interface IUpdateCheckTrigger
{
    Task<UpdateCheckResult> TriggerAsync(CancellationToken ct);
    Task<UpdateCheckResult> TriggerForceAsync(CancellationToken ct);
    Task<UpdateCheckResult> TriggerForceFullAsync(CancellationToken ct);
}

public record UpdateCheckResult(bool PackagesAvailable, string Message);
