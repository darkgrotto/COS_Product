using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CountOrSell.Data;

// Design-time factory used only by the EF Core CLI ("dotnet ef migrations", etc.)
// for tooling purposes. It is NOT used at runtime - the API resolves its connection
// string from individual DB_* env vars, POSTGRES_CONNECTION, or configuration in
// Program.cs and fails fast if none is set. This factory mirrors that resolution so
// devs can generate migrations against any configured database.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
        {
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var name = Environment.GetEnvironmentVariable("DB_NAME") ?? "countorsell";
            var user = Environment.GetEnvironmentVariable("DB_USER") ?? "countorsell";
            var pass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "countorsell";
            connection = $"Host={host};Port={port};Database={name};Username={user};Password={pass}";
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options;
        return new AppDbContext(options);
    }
}
