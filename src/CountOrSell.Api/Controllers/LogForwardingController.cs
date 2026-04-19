using CountOrSell.Api.Services.LogForwarding;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/log-forwarding")]
[Authorize(Roles = "Admin")]
public class LogForwardingController : ControllerBase
{
    private static readonly string[] ValidLevels =
        ["Trace", "Debug", "Information", "Warning", "Error", "Critical"];

    private readonly AppDbContext _db;
    private readonly LogForwardingConfigHolder _configHolder;
    private readonly ILogger<LogForwardingController> _logger;

    public LogForwardingController(
        AppDbContext db,
        LogForwardingConfigHolder configHolder,
        ILogger<LogForwardingController> logger)
    {
        _db = db;
        _configHolder = configHolder;
        _logger = logger;
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var settings = await _db.AppSettings
            .Where(s => s.Key.StartsWith("log_forwarding."))
            .ToListAsync(ct);

        var dict = settings.ToDictionary(s => s.Key, s => s.Value);
        dict.TryGetValue("log_forwarding.enabled", out var enabled);
        dict.TryGetValue("log_forwarding.url", out var url);
        dict.TryGetValue("log_forwarding.auth_header", out var authHeader);
        dict.TryGetValue("log_forwarding.min_level", out var minLevel);

        return Ok(new
        {
            enabled = enabled == "true",
            destinationUrl = string.IsNullOrEmpty(url) ? null : url,
            authHeaderSet = !string.IsNullOrEmpty(authHeader),
            minLevel = string.IsNullOrEmpty(minLevel) ? "Warning" : minLevel
        });
    }

    [HttpPut("config")]
    public async Task<IActionResult> SaveConfig(
        [FromBody] LogForwardingConfigRequest request, CancellationToken ct)
    {
        if (request.Enabled && string.IsNullOrWhiteSpace(request.DestinationUrl))
            return BadRequest(new { error = "Destination URL is required when log forwarding is enabled." });

        if (!string.IsNullOrWhiteSpace(request.DestinationUrl))
        {
            if (!Uri.TryCreate(request.DestinationUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != "http" && uri.Scheme != "https"))
                return BadRequest(new { error = "Destination URL must be a valid http or https URL." });
        }

        if (!string.IsNullOrEmpty(request.MinLevel) && !ValidLevels.Contains(request.MinLevel))
            return BadRequest(new { error = "Invalid log level." });

        var existing = await _db.AppSettings
            .Where(s => s.Key.StartsWith("log_forwarding."))
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        existing.TryGetValue("log_forwarding.auth_header", out var existingAuthHeader);
        // null AuthHeader in request = keep existing; empty string = clear
        var finalAuthHeader = request.AuthHeader == null ? existingAuthHeader : request.AuthHeader;
        var finalMinLevel = request.MinLevel ?? "Warning";

        await UpsertAsync("log_forwarding.enabled", request.Enabled ? "true" : "false", ct);
        await UpsertAsync("log_forwarding.url", request.DestinationUrl ?? string.Empty, ct);
        await UpsertAsync("log_forwarding.auth_header", finalAuthHeader ?? string.Empty, ct);
        await UpsertAsync("log_forwarding.min_level", finalMinLevel, ct);
        await _db.SaveChangesAsync(ct);

        _configHolder.Update(new LogForwardingConfig
        {
            Enabled = request.Enabled,
            DestinationUrl = request.DestinationUrl,
            AuthHeader = finalAuthHeader,
            MinLevel = finalMinLevel
        });

        return Ok();
    }

    [HttpPost("test")]
    public IActionResult SendTest()
    {
        _logger.LogWarning(
            "Log forwarding test: this entry was triggered from the Log Forwarding settings page.");
        return Ok(new { message = "Test log entry queued for forwarding." });
    }

    private async Task UpsertAsync(string key, string value, CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { key }, ct);
        if (setting != null)
            setting.Value = value;
        else
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
    }
}

public class LogForwardingConfigRequest
{
    public bool Enabled { get; init; }
    public string? DestinationUrl { get; init; }
    public string? AuthHeader { get; init; } // null = keep existing; "" = clear
    public string? MinLevel { get; init; }
}
