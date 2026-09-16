using Lutra.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Interfaces;

public interface ILutraDbContext
{
    DbSet<Supermarkt> Supermarkten { get; }

    DbSet<Beoordeling> Beoordelingen { get; }

    DbSet<VerspakketFoto> VerspakketFotos { get; }

    DbSet<Ingredient> Ingredienten { get; }

    DbSet<Voedingswaarde> Voedingswaarden { get; }

    DbSet<VerspakketAllergeen> VerspakketAllergenen { get; }

    DbSet<Verspakket> Verspaketten { get; }

    DbSet<BackgroundCommandJob> BackgroundCommandJobs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
