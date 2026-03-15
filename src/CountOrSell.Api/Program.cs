using CountOrSell.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
    ?? "Host=localhost;Database=countorsell;Username=countorsell;Password=countorsell";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("AppDbContext");

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy";
        var dbStatus = report.Entries.ContainsKey("AppDbContext") &&
            report.Entries["AppDbContext"].Status == HealthStatus.Healthy
            ? "reachable" : "unreachable";

        if (report.Status != HealthStatus.Healthy)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }

        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new { status, database = dbStatus }));
    }
});

app.Run();

public partial class Program { }
