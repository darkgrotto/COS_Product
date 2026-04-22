namespace CountOrSell.Api.Background.Updates;

public interface IUpdateCheckTrigger
{
    Task<UpdateCheckResult> TriggerAsync(CancellationToken ct);
    Task<UpdateCheckResult> TriggerForceAsync(CancellationToken ct);
    Task<UpdateCheckResult> TriggerForceFullAsync(CancellationToken ct);
    Task<UpdateCheckResult> TriggerTargetedRedownloadAsync(RedownloadOptions options, CancellationToken ct);
}

public record UpdateCheckResult(bool PackagesAvailable, string Message);

// contentType: "all" | "metadata" | "images"
// scope: "all" | "cards-sets" | "sealed"
public record RedownloadOptions(string ContentType, string Scope, bool UseFullPackage);
