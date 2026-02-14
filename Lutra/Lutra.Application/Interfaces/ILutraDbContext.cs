using Lutra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Lutra.Application.Interfaces;

public interface ILutraDbContext
{
    DbSet<Supermarkt> Supermarkten { get; }

    DbSet<Verspakket> Verspaketten { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
