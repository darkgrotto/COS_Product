namespace CountOrSell.Api.Background.Updates;

public interface IUpdateCheckTrigger
{
    Task TriggerAsync(CancellationToken ct);
}
