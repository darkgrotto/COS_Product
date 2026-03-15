using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
