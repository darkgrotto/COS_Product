using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CountOrSell.Data;

// Design-time factory used only by the EF Core CLI ("dotnet ef migrations", etc.)
// for tooling purposes. It is NOT used at runtime - the API resolves its connection
// string from POSTGRES_CONNECTION / configuration in Program.cs and fails fast if
// neither is set. This factory honors POSTGRES_CONNECTION when provided so devs can
// generate migrations against a non-local database, falling back to a local-loopback
// default for the common case where migrations are scaffolded against a dev container.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DesignTimeDefault =
        "Host=localhost;Database=countorsell;Username=countorsell;Password=countorsell";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
            connection = DesignTimeDefault;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options;
        return new AppDbContext(options);
    }
}
