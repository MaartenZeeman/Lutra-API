using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lutra.Infrastructure.Sql;

/// <summary>
/// Design-time factory used by EF Core tools (dotnet ef migrations) so that
/// no startup project or live database connection is required when running migrations.
/// </summary>
internal sealed class LutraDbContextFactory : IDesignTimeDbContextFactory<LutraDbContext>
{
    public LutraDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LutraDbContext>()
            .UseNpgsql("Host=localhost;Database=lutra;Username=postgres;Password=postgres")
            .Options;

        return new LutraDbContext(options);
    }
}
