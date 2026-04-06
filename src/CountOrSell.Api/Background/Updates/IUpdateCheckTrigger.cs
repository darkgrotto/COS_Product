namespace CountOrSell.Api.Background.Updates;

public interface IUpdateCheckTrigger
{
    Task<UpdateCheckResult> TriggerAsync(CancellationToken ct);
}

public record UpdateCheckResult(bool PackagesAvailable, string Message);
