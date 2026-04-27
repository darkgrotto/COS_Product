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

    // Instance name flows into backup filenames and labels; restrict to filesystem-safe
    // characters and a sane length so it cannot embed path-traversal sequences. Even
    // though backup filenames now derive from record GUIDs, this stays as defense in
    // depth: any future caller that uses InstanceName in a path must not be exploitable.
    private static readonly System.Text.RegularExpressions.Regex InstanceNameRegex =
        new(@"^[A-Za-z0-9][A-Za-z0-9 _\-.]{0,63}$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    [HttpPatch("instance")]
    [DemoLocked]
    public async Task<IActionResult> UpdateInstanceSettings(
        [FromBody] InstanceSettingsRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.InstanceName))
            return BadRequest(new { error = "Instance name is required." });

        var trimmed = request.InstanceName.Trim();
        if (!InstanceNameRegex.IsMatch(trimmed) || trimmed.Contains(".."))
            return BadRequest(new
            {
                error = "Instance name must start with a letter or digit and may " +
                        "contain only letters, digits, spaces, underscores, hyphens, " +
                        "and dots (max 64 characters; consecutive dots not allowed)."
            });

        await UpsertSettingAsync("instance_name", trimmed, ct);
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
        var result = new List<object>();
        foreach (var (provider, clientIdKey, secretKey, tenantKey) in OAuthProviderKeys())
        {
            var clientId = await GetSettingAsync(clientIdKey, "", ct);
            var secret   = await GetSettingAsync(secretKey,   "", ct);
            var tenantId = tenantKey != null ? await GetSettingAsync(tenantKey, "", ct) : "";
            result.Add(new
            {
                provider,
                clientId          = string.IsNullOrWhiteSpace(clientId) ? null : clientId,
                secretConfigured  = !string.IsNullOrWhiteSpace(secret),
                tenantId          = tenantKey == null
                    ? null
                    : string.IsNullOrWhiteSpace(tenantId) ? null : tenantId,
                requiresTenantId  = tenantKey != null,
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
        var keys = OAuthProviderKeys()
            .FirstOrDefault(p => string.Equals(p.Provider, provider, StringComparison.OrdinalIgnoreCase));
        if (keys.Provider == null)
            return BadRequest(new { error = $"Unknown OAuth provider: {provider}" });

        if (!string.IsNullOrWhiteSpace(request.ClientId))
            await UpsertSettingAsync(keys.ClientIdKey, request.ClientId.Trim(), ct);

        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
            await UpsertSettingAsync(keys.SecretKey, request.ClientSecret.Trim(), ct);

        if (!string.IsNullOrWhiteSpace(request.TenantId))
        {
            if (keys.TenantIdKey == null)
                return BadRequest(new { error = $"Provider '{provider}' does not accept a tenant id." });
            await UpsertSettingAsync(keys.TenantIdKey, request.TenantId.Trim(), ct);
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("oauth/{provider}")]
    [DemoLocked]
    public async Task<IActionResult> ClearOAuthProvider(string provider, CancellationToken ct)
    {
        var keys = OAuthProviderKeys()
            .FirstOrDefault(p => string.Equals(p.Provider, provider, StringComparison.OrdinalIgnoreCase));
        if (keys.Provider == null)
            return BadRequest(new { error = $"Unknown OAuth provider: {provider}" });

        foreach (var key in new[] { keys.ClientIdKey, keys.SecretKey, keys.TenantIdKey })
        {
            if (key == null) continue;
            var entity = await _db.AppSettings.FindAsync(new object[] { key }, ct);
            if (entity != null) _db.AppSettings.Remove(entity);
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    private static (string Provider, string ClientIdKey, string SecretKey, string? TenantIdKey)[] OAuthProviderKeys() =>
        new (string, string, string, string?)[]
        {
            ("google",          "oauth_google_client_id",            "oauth_google_client_secret",          null),
            ("microsoft",       "oauth_microsoft_client_id",         "oauth_microsoft_client_secret",       null),
            ("microsoft-entra", "oauth_microsoft_entra_client_id",   "oauth_microsoft_entra_client_secret", "oauth_microsoft_entra_tenant_id"),
            ("github",          "oauth_github_client_id",            "oauth_github_client_secret",          null),
        };

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
    public string? TenantId { get; set; }
}
