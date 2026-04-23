using Lutra.Domain.Entities;
using Lutra.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<LutraDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LutraDb")));

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<LutraDbContext>();
await dbContext.Database.MigrateAsync();

if (!await dbContext.Supermarkten.AnyAsync())
{
    var createdAt = DateTime.UtcNow;
    dbContext.Supermarkten.AddRange(
        CreateSupermarkt("Albert Heijn", createdAt),
        CreateSupermarkt("Jumbo", createdAt),
        CreateSupermarkt("Poiesz", createdAt),
        CreateSupermarkt("Lidl", createdAt));

    await dbContext.SaveChangesAsync();
}

static Supermarkt CreateSupermarkt(string naam, DateTime createdAt)
{
    return new Supermarkt
    {
        Id = Guid.NewGuid(),
        Naam = naam,
        CreatedAt = createdAt,
        ModifiedAt = createdAt
    };
}
