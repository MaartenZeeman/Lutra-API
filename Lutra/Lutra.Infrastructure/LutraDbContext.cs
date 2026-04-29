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

    public DbSet<VerspakketFoto> VerspakketFotos => Set<VerspakketFoto>();

    public DbSet<Verspakket> Verspaketten => Set<Verspakket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global soft-delete filter: exclude logically deleted entities from all queries.
        modelBuilder.Entity<Beoordeling>().HasQueryFilter(b => !b.IsDeleted);
        modelBuilder.Entity<VerspakketFoto>().HasQueryFilter(f => !f.IsDeleted);
        modelBuilder.Entity<Supermarkt>().HasQueryFilter(s => !s.IsDeleted);

        modelBuilder.Entity<Verspakket>(b =>
        {
            b.HasQueryFilter(v => !v.IsDeleted);

            b.HasMany(v => v.Beoordelingen)
                .WithOne()
                .HasForeignKey(beo => beo.VerspakketId)
                .IsRequired();

            b.HasMany(v => v.Fotos)
                .WithOne()
                .HasForeignKey(foto => foto.VerspakketId)
                .IsRequired();

            b.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Verspaketten_AantalPersonen", "\"AantalPersonen\" BETWEEN 1 AND 10");
                t.HasCheckConstraint("CK_Verspaketten_PrijsInCenten", "\"PrijsInCenten\" IS NULL OR \"PrijsInCenten\" >= 0");
            });
        });
    }

    /// <summary>
    /// Populates audit fields on tracked entities before persisting.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.ModifiedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
