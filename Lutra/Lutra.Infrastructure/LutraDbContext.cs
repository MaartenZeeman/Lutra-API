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

    public DbSet<Voedingswaarde> Voedingswaarden => Set<Voedingswaarde>();

    public DbSet<VerspakketAllergeen> VerspakketAllergenen => Set<VerspakketAllergeen>();

    public DbSet<Verspakket> Verspaketten => Set<Verspakket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global soft-delete filter: exclude logically deleted entities from all queries.
        // Filters on DeletedAt directly because the derived IsDeleted getter is not translatable.
        modelBuilder.Entity<Beoordeling>().HasQueryFilter(b => !b.DeletedAt.HasValue);
        modelBuilder.Entity<VerspakketFoto>().HasQueryFilter(f => !f.DeletedAt.HasValue);
        modelBuilder.Entity<Ingredient>().HasQueryFilter(i => !i.DeletedAt.HasValue);
        modelBuilder.Entity<Voedingswaarde>().HasQueryFilter(w => !w.DeletedAt.HasValue);
        modelBuilder.Entity<VerspakketAllergeen>().HasQueryFilter(a => !a.DeletedAt.HasValue);
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

            b.HasOne(v => v.Voedingswaarde)
                .WithOne(w => w.Verspakket)
                .HasForeignKey<Voedingswaarde>(w => w.VerspakketId)
                .IsRequired();

            b.HasMany(v => v.Allergenen)
                .WithOne(a => a.Verspakket)
                .HasForeignKey(a => a.VerspakketId)
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

        modelBuilder.Entity<Voedingswaarde>(b =>
        {
            b.Property(w => w.EnergieKj).HasPrecision(10, 2);
            b.Property(w => w.EnergieKcal).HasPrecision(10, 2);
            b.Property(w => w.Vetten).HasPrecision(10, 2);
            b.Property(w => w.WaarvanVerzadigd).HasPrecision(10, 2);
            b.Property(w => w.Koolhydraten).HasPrecision(10, 2);
            b.Property(w => w.WaarvanSuikers).HasPrecision(10, 2);
            b.Property(w => w.Vezels).HasPrecision(10, 2);
            b.Property(w => w.Eiwitten).HasPrecision(10, 2);
            b.Property(w => w.Zout).HasPrecision(10, 2);

            b.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Voedingswaarden_EnergieKj", "\"EnergieKj\" IS NULL OR \"EnergieKj\" >= 0");
                t.HasCheckConstraint("CK_Voedingswaarden_EnergieKcal", "\"EnergieKcal\" IS NULL OR \"EnergieKcal\" >= 0");
                t.HasCheckConstraint("CK_Voedingswaarden_Vetten", "\"Vetten\" IS NULL OR \"Vetten\" >= 0");
                // CAST to NUMERIC because SQLite (used in integration tests) stores decimal as TEXT,
                // which would make a direct comparison lexicographic instead of numeric.
                t.HasCheckConstraint("CK_Voedingswaarden_WaarvanVerzadigd", "\"WaarvanVerzadigd\" IS NULL OR CAST(\"Vetten\" AS NUMERIC) IS NULL OR CAST(\"WaarvanVerzadigd\" AS NUMERIC) <= CAST(\"Vetten\" AS NUMERIC)");
                t.HasCheckConstraint("CK_Voedingswaarden_Koolhydraten", "\"Koolhydraten\" IS NULL OR \"Koolhydraten\" >= 0");
                t.HasCheckConstraint("CK_Voedingswaarden_WaarvanSuikers", "\"WaarvanSuikers\" IS NULL OR CAST(\"Koolhydraten\" AS NUMERIC) IS NULL OR CAST(\"WaarvanSuikers\" AS NUMERIC) <= CAST(\"Koolhydraten\" AS NUMERIC)");
                t.HasCheckConstraint("CK_Voedingswaarden_Vezels", "\"Vezels\" IS NULL OR \"Vezels\" >= 0");
                t.HasCheckConstraint("CK_Voedingswaarden_Eiwitten", "\"Eiwitten\" IS NULL OR \"Eiwitten\" >= 0");
                t.HasCheckConstraint("CK_Voedingswaarden_Zout", "\"Zout\" IS NULL OR \"Zout\" >= 0");
            });
        });

        modelBuilder.Entity<VerspakketAllergeen>(b =>
        {
            b.HasIndex(a => new { a.VerspakketId, a.Allergeen }).IsUnique();
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
