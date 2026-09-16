using Lutra.Application.Interfaces;
using Lutra.Infrastructure.Sql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lutra.API.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that replaces the PostgreSQL database with SQLite in-memory
/// so that integration tests can run without a live database server.
/// A single SqliteConnection is kept open for the lifetime of the factory so that
/// all DI scopes share the same in-memory database.
/// </summary>
public class LutraApiFactory : WebApplicationFactory<Program>
{
    // Opened immediately so it is ready when ConfigureWebHost runs.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private bool _schemaCreated;

    public LutraApiFactory()
    {
        _connection.Open();
    }

    /// <summary>Ensures the SQLite schema is created. Call once before the first test.</summary>
    public void EnsureSchemaCreated()
    {
        if (_schemaCreated) return;

        using var scope = Services.CreateScope();
        var db = (LutraDbContext)scope.ServiceProvider.GetRequiredService<ILutraDbContext>();
        db.Database.EnsureCreated();
        _schemaCreated = true;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // EF Core 10 stores provider configuration in IDbContextOptionsConfiguration<T>
            // descriptors (one per AddDbContext call). All four registration types must be
            // removed so neither Npgsql options nor its provider services survive into the
            // SQLite registration.
            services.RemoveAll<ILutraDbContext>();
            services.RemoveAll<LutraDbContext>();
            services.RemoveAll<DbContextOptions<LutraDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<LutraDbContext>));

            // Register SQLite using the shared open connection.
            services.AddDbContext<ILutraDbContext, LutraDbContext>(options =>
                options.UseSqlite(_connection));

            // Never call the real AI provider from integration tests.
            services.RemoveAll<IVerspakketProductExtractor>();
            services.AddSingleton<IVerspakketProductExtractor, FakeVerspakketProductExtractor>();
        });

        builder.UseEnvironment("Testing");

        // The worker is disabled so tests can trigger processing deterministically; retries run immediately.
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundCommands:Enabled"] = "false",
                ["BackgroundCommands:RetryDelayMinutes"] = "0",
                ["BackgroundCommands:MaxAttempts"] = "3",
                ["BackgroundCommands:LeaseDurationMinutes"] = "10"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
