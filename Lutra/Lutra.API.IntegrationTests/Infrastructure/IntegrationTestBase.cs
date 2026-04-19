using Lutra.Application.Interfaces;
using Lutra.Infrastructure.Sql;
using Microsoft.Extensions.DependencyInjection;

namespace Lutra.API.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests. Provides a shared factory and helper methods
/// to seed and reset the database between tests.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<LutraApiFactory>, IAsyncLifetime
{
    protected readonly LutraApiFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(LutraApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>Seed data or perform setup before each test.</summary>
    public virtual Task InitializeAsync()
    {
        Factory.EnsureSchemaCreated();
        return Task.CompletedTask;
    }

    /// <summary>Reset database state after each test.</summary>
    public async Task DisposeAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ILutraDbContext>() as LutraDbContext;
        if (db is not null)
        {
            db.Beoordelingen.RemoveRange(db.Beoordelingen);
            db.Verspaketten.RemoveRange(db.Verspaketten);
            db.Supermarkten.RemoveRange(db.Supermarkten);
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    protected async Task<T> SeedAsync<T>(T entity) where T : class
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ILutraDbContext>() as LutraDbContext;
        db!.Set<T>().Add(entity);
        await db.SaveChangesAsync(CancellationToken.None);
        return entity;
    }
}
