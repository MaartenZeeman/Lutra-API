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

    public DbSet<Beoordeling> Beoordelingen => Set<Beoordeling>();

    public DbSet<Verspakket> Verspaketten => Set<Verspakket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Beoordeling>()
            .ToTable("Beoordelingen");

        modelBuilder.Entity<Verspakket>(b =>
        {
            b.HasMany(v => v.Beoordelingen)
                .WithOne()
                .HasForeignKey(beo => beo.VerspakketId)
                .IsRequired();

            b.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Verspaketten_AantalPersonen", "\"AantalPersonen\" BETWEEN 1 AND 10");
                t.HasCheckConstraint("CK_Verspaketten_PrijsInCenten", "\"PrijsInCenten\" IS NULL OR \"PrijsInCenten\" >= 0");
            });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
