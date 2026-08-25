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

    public DbSet<Ingredient> Ingredienten => Set<Ingredient>();

    public DbSet<Verspakket> Verspaketten => Set<Verspakket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global soft-delete filter: exclude logically deleted entities from all queries.
        // Filters on DeletedAt directly because the derived IsDeleted getter is not translatable.
        modelBuilder.Entity<Beoordeling>().HasQueryFilter(b => !b.DeletedAt.HasValue);
        modelBuilder.Entity<VerspakketFoto>().HasQueryFilter(f => !f.DeletedAt.HasValue);
        modelBuilder.Entity<Ingredient>().HasQueryFilter(i => !i.DeletedAt.HasValue);
        modelBuilder.Entity<Supermarkt>().HasQueryFilter(s => !s.DeletedAt.HasValue);

        modelBuilder.Entity<Verspakket>(b =>
        {
            b.HasQueryFilter(v => !v.DeletedAt.HasValue);

            b.HasMany(v => v.Beoordelingen)
                .WithOne(beo => beo.Verspakket)
                .HasForeignKey(beo => beo.VerspakketId)
                .IsRequired();

            b.HasMany(v => v.Fotos)
                .WithOne(foto => foto.Verspakket)
                .HasForeignKey(foto => foto.VerspakketId)
                .IsRequired();

            b.HasMany(v => v.Ingredienten)
                .WithOne(ing => ing.Verspakket)
                .HasForeignKey(ing => ing.VerspakketId)
                .IsRequired();

            b.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Verspaketten_AantalPersonen", "\"AantalPersonen\" BETWEEN 1 AND 10");
                t.HasCheckConstraint("CK_Verspaketten_PrijsInCenten", "\"PrijsInCenten\" IS NULL OR \"PrijsInCenten\" >= 0");
            });
        });

        modelBuilder.Entity<Ingredient>(b =>
        {
            b.Property(i => i.Hoeveelheid).HasPrecision(10, 2);

            b.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Ingredienten_Hoeveelheid", "\"Hoeveelheid\" > 0");
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
