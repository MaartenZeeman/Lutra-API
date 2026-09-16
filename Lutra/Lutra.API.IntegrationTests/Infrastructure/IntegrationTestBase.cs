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
    public virtual ValueTask InitializeAsync()
    {
        Factory.EnsureSchemaCreated();
        return ValueTask.CompletedTask;
    }

    /// <summary>Reset database state after each test.</summary>
    public async ValueTask DisposeAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ILutraDbContext>() as LutraDbContext;
        if (db is not null)
        {
            db.BackgroundCommandJobs.RemoveRange(db.BackgroundCommandJobs);
            db.Beoordelingen.RemoveRange(db.Beoordelingen);
            db.Verspaketten.RemoveRange(db.Verspaketten);
            db.Supermarkten.RemoveRange(db.Supermarkten);
            await db.SaveChangesAsync(CancellationToken.None);
        }   
    }

    protected async ValueTask<T> SeedAsync<T>(T entity) where T : class
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ILutraDbContext>() as LutraDbContext;
        db!.Set<T>().Add(entity);
        await db.SaveChangesAsync(CancellationToken.None);
        return entity;
    }
}
