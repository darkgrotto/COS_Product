using System.Security.Claims;
using CountOrSell.Api.Filters;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IAuditLogger _audit;

    public SettingsController(AppDbContext db, IConfiguration config, IAuditLogger audit)
    {
        _db = db;
        _config = config;
        _audit = audit;
    }

    private string ActorName => User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
    private string ActorDisplayName => User.FindFirstValue("display_name") ?? ActorName;
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet("backup")]
    public async Task<IActionResult> GetBackupSettings(CancellationToken ct)
    {
        var schedule = await GetSettingAsync("backup_schedule", "weekly", ct);
        var retentionScheduled = await GetSettingAsync("backup_retention_scheduled", "4", ct);
        var retentionPreUpdate = await GetSettingAsync("backup_retention_pre_update", "4", ct);

        return Ok(new
        {
            schedule,
            retentionScheduled = int.TryParse(retentionScheduled, out var rs) ? rs : 4,
            retentionPreUpdate = int.TryParse(retentionPreUpdate, out var rp) ? rp : 4
        });
    }

    [HttpPatch("backup")]
    public async Task<IActionResult> UpdateBackupSettings(
        [FromBody] BackupSettingsRequest request,
        CancellationToken ct)
    {
        if (request.Schedule != null)
            await UpsertSettingAsync("backup_schedule", request.Schedule, ct);

        if (request.RetentionScheduled.HasValue)
            await UpsertSettingAsync(
                "backup_retention_scheduled",
                request.RetentionScheduled.Value.ToString(),
                ct);

        if (request.RetentionPreUpdate.HasValue)
            await UpsertSettingAsync(
                "backup_retention_pre_update",
                request.RetentionPreUpdate.Value.ToString(),
                ct);

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpGet("instance")]
    public async Task<IActionResult> GetInstanceSettings(CancellationToken ct)
    {
        var instanceName = await GetSettingAsync("instance_name", "", ct);
        if (string.IsNullOrEmpty(instanceName))
            instanceName = _config["INSTANCE_NAME"] ?? "";
        return Ok(new { instanceName });
    }

    [HttpPatch("instance")]
    [DemoLocked]
    public async Task<IActionResult> UpdateInstanceSettings(
        [FromBody] InstanceSettingsRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.InstanceName))
            return BadRequest(new { error = "Instance name is required." });

        await UpsertSettingAsync("instance_name", request.InstanceName.Trim(), ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(ActorName, ActorDisplayName, "settings.instance", "instance_name", "success", ClientIp);
        return Ok();
    }

    [HttpGet("tcgplayer")]
    public async Task<IActionResult> GetTcgPlayerSettings(CancellationToken ct)
    {
        var key = await GetSettingAsync("tcgplayer_api_key", "", ct);
        var configured = !string.IsNullOrWhiteSpace(key);
        var maskedKey = configured ? MaskApiKey(key) : null;
        return Ok(new { configured, maskedKey });
    }

    [HttpPut("tcgplayer")]
    public async Task<IActionResult> SetTcgPlayerKey(
        [FromBody] TcgPlayerKeyRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            return BadRequest(new { error = "API key is required." });

        await UpsertSettingAsync("tcgplayer_api_key", request.ApiKey.Trim(), ct);
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("tcgplayer")]
    public async Task<IActionResult> ClearTcgPlayerKey(CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { "tcgplayer_api_key" }, ct);
        if (setting != null)
            _db.AppSettings.Remove(setting);
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpGet("self-enrollment")]
    public async Task<IActionResult> GetSelfEnrollment(CancellationToken ct)
    {
        var val = await GetSettingAsync("self_enrollment_enabled", "false", ct);
        return Ok(new { enabled = val == "true" });
    }

    [HttpPatch("self-enrollment")]
    [DemoLocked]
    public async Task<IActionResult> UpdateSelfEnrollment(
        [FromBody] SelfEnrollmentRequest request,
        CancellationToken ct)
    {
        await UpsertSettingAsync("self_enrollment_enabled", request.Enabled ? "true" : "false", ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(ActorName, ActorDisplayName, "settings.self-enrollment",
            request.Enabled ? "enabled" : "disabled", "success", ClientIp);
        return Ok();
    }

    [HttpGet("oauth")]
    public async Task<IActionResult> GetOAuthSettings(CancellationToken ct)
    {
        var providers = new[]
        {
            ("google",    "oauth_google_client_id",    "oauth_google_client_secret"),
            ("microsoft", "oauth_microsoft_client_id", "oauth_microsoft_client_secret"),
            ("github",    "oauth_github_client_id",    "oauth_github_client_secret"),
        };

        var result = new List<object>();
        foreach (var (provider, clientIdKey, secretKey) in providers)
        {
            var clientId = await GetSettingAsync(clientIdKey, "", ct);
            var secret   = await GetSettingAsync(secretKey,   "", ct);
            result.Add(new
            {
                provider,
                clientId          = string.IsNullOrWhiteSpace(clientId) ? null : clientId,
                secretConfigured  = !string.IsNullOrWhiteSpace(secret),
            });
        }
        return Ok(result);
    }

    [HttpPatch("oauth/{provider}")]
    [DemoLocked]
    public async Task<IActionResult> UpdateOAuthProvider(
        string provider,
        [FromBody] OAuthProviderRequest request,
        CancellationToken ct)
    {
        var (clientIdKey, secretKey) = provider.ToLowerInvariant() switch
        {
            "google"    => ("oauth_google_client_id",    "oauth_google_client_secret"),
            "microsoft" => ("oauth_microsoft_client_id", "oauth_microsoft_client_secret"),
            "github"    => ("oauth_github_client_id",    "oauth_github_client_secret"),
            _           => (null, null),
        };

        if (clientIdKey == null)
            return BadRequest(new { error = $"Unknown OAuth provider: {provider}" });

        if (!string.IsNullOrWhiteSpace(request.ClientId))
            await UpsertSettingAsync(clientIdKey, request.ClientId.Trim(), ct);

        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
            await UpsertSettingAsync(secretKey!, request.ClientSecret.Trim(), ct);

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("oauth/{provider}")]
    [DemoLocked]
    public async Task<IActionResult> ClearOAuthProvider(string provider, CancellationToken ct)
    {
        var (clientIdKey, secretKey) = provider.ToLowerInvariant() switch
        {
            "google"    => ("oauth_google_client_id",    "oauth_google_client_secret"),
            "microsoft" => ("oauth_microsoft_client_id", "oauth_microsoft_client_secret"),
            "github"    => ("oauth_github_client_id",    "oauth_github_client_secret"),
            _           => (null, null),
        };

        if (clientIdKey == null)
            return BadRequest(new { error = $"Unknown OAuth provider: {provider}" });

        var clientIdSetting = await _db.AppSettings.FindAsync(new object[] { clientIdKey }, ct);
        if (clientIdSetting != null)
            _db.AppSettings.Remove(clientIdSetting);

        var secretSetting = await _db.AppSettings.FindAsync(new object[] { secretKey! }, ct);
        if (secretSetting != null)
            _db.AppSettings.Remove(secretSetting);

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    private static string MaskApiKey(string key)
    {
        if (key.Length <= 4) return new string('*', key.Length);
        return new string('*', key.Length - 4) + key[^4..];
    }

    private async Task<string> GetSettingAsync(
        string key, string defaultValue, CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { key }, ct);
        return setting?.Value ?? defaultValue;
    }

    private async Task UpsertSettingAsync(string key, string value, CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { key }, ct);
        if (setting != null)
        {
            setting.Value = value;
        }
        else
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
    }
}

public class BackupSettingsRequest
{
    public string? Schedule { get; set; }
    public int? RetentionScheduled { get; set; }
    public int? RetentionPreUpdate { get; set; }
}

public class InstanceSettingsRequest
{
    public string InstanceName { get; set; } = string.Empty;
}

public class TcgPlayerKeyRequest
{
    public string ApiKey { get; set; } = string.Empty;
}

public class SelfEnrollmentRequest
{
    public bool Enabled { get; set; }
}

public class OAuthProviderRequest
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}
