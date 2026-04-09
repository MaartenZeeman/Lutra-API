using Lutra.Application.Interfaces;
using Lutra.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Infrastructure.Sql;

public class LutraDbContext : DbContext, ILutraDbContext
{
    public LutraDbContext(DbContextOptions<LutraDbContext> options)
        : base(options)
    {
    }

    public DbSet<Supermarkt> Supermarkten => Set<Supermarkt>();

    public DbSet<Verspakket> Verspaketten => Set<Verspakket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Beoordeling>()
            .ToTable("Beoordelingen");

        modelBuilder.Entity<Verspakket>()
            .HasMany(v => v.Beoordelingen)
            .WithOne()
            .HasForeignKey(b => b.VerspakketId)
            .IsRequired();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
