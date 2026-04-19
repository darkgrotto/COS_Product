namespace CountOrSell.Api.Services.LogForwarding;

public class LogForwardingConfig
{
    public bool Enabled { get; init; }
    public string? DestinationUrl { get; init; }
    public string? AuthHeader { get; init; }
    public string MinLevel { get; init; } = "Warning";
}
