using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using CountOrSell.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CountOrSell.Tests.Integration.Import;

// Header-driven auth so each test can speak as a stable user across the
// csrf handshake and the subsequent state-changing request.
public class ImportTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public ImportTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User-Id", out var userIdVal))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = Guid.TryParse(userIdVal, out var parsed) ? parsed : Guid.NewGuid();
        var role = Request.Headers.TryGetValue("X-Test-User-Role", out var roleVal)
            ? roleVal.ToString()
            : "GeneralUser";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, $"user-{userId}"),
            new Claim(ClaimTypes.Role, role),
        };
        var identity = new ClaimsIdentity(claims, "ImportTest");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "ImportTest");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal static class ImportTestSupport
{
    public static readonly Guid TestUserId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

    public static WebApplicationFactory<Program> BuildFactory(
        WebApplicationFactory<Program> baseFactory,
        string dbName,
        IEnumerable<string> validTreatments)
    {
        return baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var dbDesc = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDesc != null) services.Remove(dbDesc);
                var ctxDesc = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
                if (ctxDesc != null) services.Remove(ctxDesc);

                services.AddDbContext<AppDbContext>(
                    opt => opt.UseInMemoryDatabase(dbName),
                    optionsLifetime: ServiceLifetime.Singleton);

                var tvDesc = services.SingleOrDefault(d => d.ServiceType == typeof(ITreatmentValidator));
                if (tvDesc != null) services.Remove(tvDesc);
                services.AddSingleton<ITreatmentValidator>(new FixedTreatmentValidator(validTreatments));

                services.AddAuthentication("ImportTest")
                    .AddScheme<AuthenticationSchemeOptions, ImportTestAuthHandler>(
                        "ImportTest", _ => { });
            });
        });
    }

    public static async Task SeedAsync(
        WebApplicationFactory<Program> factory,
        Action<AppDbContext> seed)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(new User
        {
            Id = TestUserId,
            Username = "importuser",
            PasswordHash = "x",
            AuthType = AuthType.Local,
            Role = UserRole.GeneralUser,
            IsBuiltinAdmin = false,
            State = AccountState.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        seed(db);
        await db.SaveChangesAsync();
    }

    public static async Task<HttpClient> ClientWithCsrfAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        client.DefaultRequestHeaders.Add("X-Test-User-Id", TestUserId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-User-Role", "GeneralUser");

        var csrf = await client.GetAsync("/api/auth/csrf");
        csrf.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Add(
            "X-CSRF-TOKEN", doc.RootElement.GetProperty("token").GetString());
        return client;
    }

    public static HttpClient ClientWithoutAuth(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

    public static MultipartFormDataContent CsvPayload(string csv, string fileName = "import.csv")
    {
        var form = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        bytes.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        form.Add(bytes, "file", fileName);
        return form;
    }

    public static async Task<ImportResponse> ParseResponseAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ImportResponse>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        })!;
    }
}

public record ImportResponse(int Added, int Skipped, int Failed, List<string> Failures);
