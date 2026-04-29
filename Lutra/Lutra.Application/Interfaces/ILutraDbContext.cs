using Lutra.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Interfaces;

public interface ILutraDbContext
{
    DbSet<Supermarkt> Supermarkten { get; }

    DbSet<Beoordeling> Beoordelingen { get; }

    DbSet<VerspakketFoto> VerspakketFotos { get; }

    DbSet<Verspakket> Verspaketten { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
