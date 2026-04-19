using System.Net.Http.Json;
using System.Threading.Channels;

namespace CountOrSell.Api.Services.LogForwarding;

internal record LogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Category,
    string Message,
    string? Exception);

public sealed class HttpLogForwardingProvider : ILoggerProvider
{
    private readonly LogForwardingConfigHolder _configHolder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Channel<LogEntry> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _flushTask;

    public HttpLogForwardingProvider(
        LogForwardingConfigHolder configHolder,
        IHttpClientFactory httpClientFactory)
    {
        _configHolder = configHolder;
        _httpClientFactory = httpClientFactory;
        _channel = Channel.CreateBounded<LogEntry>(
            new BoundedChannelOptions(5000) { FullMode = BoundedChannelFullMode.DropOldest });
        _flushTask = Task.Run(() => FlushLoopAsync(_cts.Token));
    }

    public ILogger CreateLogger(string categoryName)
        => new HttpForwardingLogger(categoryName, _configHolder, _channel.Writer);

    private async Task FlushLoopAsync(CancellationToken ct)
    {
        var batch = new List<LogEntry>(100);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linkedCts.CancelAfter(TimeSpan.FromSeconds(10));
                try
                {
                    await _channel.Reader.WaitToReadAsync(linkedCts.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }

                while (batch.Count < 100 && _channel.Reader.TryRead(out var entry))
                    batch.Add(entry);

                if (batch.Count > 0)
                {
                    var config = _configHolder.Current;
                    if (config.Enabled && !string.IsNullOrEmpty(config.DestinationUrl))
                        await TrySendBatchAsync(batch, config);
                    batch.Clear();
                }
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                batch.Clear();
                await Task.Delay(5000, CancellationToken.None);
            }
        }

        // Drain remaining entries on shutdown
        while (_channel.Reader.TryRead(out var entry))
            batch.Add(entry);
        if (batch.Count > 0)
        {
            var config = _configHolder.Current;
            if (config.Enabled && !string.IsNullOrEmpty(config.DestinationUrl))
                await TrySendBatchAsync(batch, config);
        }
    }

    private async Task TrySendBatchAsync(List<LogEntry> batch, LogForwardingConfig config)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LogForwarding");
            var payload = batch.Select(e => new
            {
                timestamp = e.Timestamp,
                level = e.Level,
                category = e.Category,
                message = e.Message,
                exception = e.Exception
            });
            var request = new HttpRequestMessage(HttpMethod.Post, config.DestinationUrl)
            {
                Content = JsonContent.Create(payload)
            };
            if (!string.IsNullOrEmpty(config.AuthHeader))
                request.Headers.TryAddWithoutValidation("Authorization", config.AuthHeader);
            using var response = await client.SendAsync(request, CancellationToken.None);
        }
        catch { /* Best-effort forwarding - failures are silently dropped */ }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _flushTask.Wait(TimeSpan.FromSeconds(5)); } catch { }
        _cts.Dispose();
    }
}

internal sealed class HttpForwardingLogger : ILogger
{
    private readonly string _category;
    private readonly LogForwardingConfigHolder _configHolder;
    private readonly ChannelWriter<LogEntry> _writer;

    internal HttpForwardingLogger(
        string category,
        LogForwardingConfigHolder configHolder,
        ChannelWriter<LogEntry> writer)
    {
        _category = category;
        _configHolder = configHolder;
        _writer = writer;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        var config = _configHolder.Current;
        return config.Enabled && logLevel >= GetMinLevel(config.MinLevel);
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        var entry = new LogEntry(
            DateTimeOffset.UtcNow,
            logLevel.ToString(),
            _category,
            formatter(state, exception),
            exception?.ToString());
        _writer.TryWrite(entry);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    private static LogLevel GetMinLevel(string level) => level switch
    {
        "Trace" => LogLevel.Trace,
        "Debug" => LogLevel.Debug,
        "Information" => LogLevel.Information,
        "Warning" => LogLevel.Warning,
        "Error" => LogLevel.Error,
        "Critical" => LogLevel.Critical,
        _ => LogLevel.Warning
    };
}
