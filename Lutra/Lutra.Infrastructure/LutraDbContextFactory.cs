using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lutra.Infrastructure.Sql;

/// <summary>
/// Design-time factory used by EF Core tools (dotnet ef migrations) so that
/// no startup project or live database connection is required when running migrations.
/// Reads ConnectionStrings:LutraDb from the API project's appsettings.json.
/// </summary>
internal sealed class LutraDbContextFactory : IDesignTimeDbContextFactory<LutraDbContext>
{
    private const string ConnectionStringName = "LutraDb";
    private const string ConnectionStringEnvironmentVariableName = "ConnectionStrings__LutraDb";

    public LutraDbContext CreateDbContext(string[] args)
    {
        var apiProjectPath = GetApiProjectPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("<set-locally>", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} is not configured. Set it in appsettings.Development.json or via {ConnectionStringEnvironmentVariableName}.");
        }

        var options = new DbContextOptionsBuilder<LutraDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new LutraDbContext(options);
    }

    private static string GetApiProjectPath()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDirectory is not null)
        {
            var apiProjectPath = Path.Combine(currentDirectory.FullName, "Lutra.API");
            if (Directory.Exists(apiProjectPath))
            {
                return apiProjectPath;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the Lutra.API project directory.");
    }
}
