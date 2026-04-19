namespace CountOrSell.Api.Services.LogForwarding;

public class LogForwardingConfigHolder
{
    private volatile LogForwardingConfig _current = new();
    public LogForwardingConfig Current => _current;
    public void Update(LogForwardingConfig config) => _current = config;
}
