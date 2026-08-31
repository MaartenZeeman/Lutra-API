using Lutra.Domain.Entities;
using Lutra.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Infrastructure.Migrator;

/// <summary>
/// Seeds the database with the known supermarkten and the Jumbo Satépannetje verspakket.
/// Reconciles on every run: existing seeded entities are re-applied (scalars, voedingswaarde,
/// ingredienten and allergenen are refreshed), so whenever a relevant entity changes,
/// the next migrator run updates the seed again.
/// </summary>
public static class LutraSeeder
{
    private const string SatepannetjeNaam = "Jumbo Satépannetje Gesneden Verspakket 4 Personen";
    private const int SatepannetjePrijsInCenten = 749;
    private const int SatepannetjeAantalPersonen = 4;

    public static async Task SeedAsync(LutraDbContext dbContext)
    {
        var now = DateTime.UtcNow;

        EnsureSupermarkt(dbContext, "Albert Heijn", now);
        var jumbo = EnsureSupermarkt(dbContext, "Jumbo", now);
        EnsureSupermarkt(dbContext, "Poiesz", now);
        EnsureSupermarkt(dbContext, "Lidl", now);
        await dbContext.SaveChangesAsync();

        await UpsertSatepannetjeAsync(dbContext, jumbo, now);
        await dbContext.SaveChangesAsync();
    }

    private static Supermarkt EnsureSupermarkt(LutraDbContext dbContext, string naam, DateTime now)
    {
        var supermarkt = dbContext.Supermarkten.FirstOrDefault(s => s.Naam == naam);
        if (supermarkt is not null)
        {
            return supermarkt;
        }

        supermarkt = new Supermarkt
        {
            Id = Guid.NewGuid(),
            Naam = naam,
            CreatedAt = now,
            ModifiedAt = now
        };

        dbContext.Supermarkten.Add(supermarkt);
        return supermarkt;
    }

    private static async Task UpsertSatepannetjeAsync(LutraDbContext dbContext, Supermarkt jumbo, DateTime now)
    {
        var verspakket = await dbContext.Verspaketten
            .Include(v => v.Voedingswaarde)
            .FirstOrDefaultAsync(v => v.Naam == SatepannetjeNaam && v.DeletedAt == null);

        if (verspakket is null)
        {
            verspakket = new Verspakket
            {
                Id = Guid.NewGuid(),
                Naam = SatepannetjeNaam,
                PrijsInCenten = SatepannetjePrijsInCenten,
                AantalPersonen = SatepannetjeAantalPersonen,
                SupermarktId = jumbo.Id,
                CreatedAt = now,
                ModifiedAt = now
            };

            verspakket.Voedingswaarde = CreateVoedingswaarde(verspakket.Id, now);
            foreach (var ingredient in CreateIngredienten(verspakket.Id, now))
            {
                verspakket.AddIngredient(ingredient);
            }

            foreach (var allergeen in CreateAllergenen(verspakket.Id, now))
            {
                verspakket.AddAllergeen(allergeen);
            }

            dbContext.Verspaketten.Add(verspakket);
            return;
        }

        // Re-apply the seed values so changed entities are updated back to the seed definition.
        verspakket.PrijsInCenten = SatepannetjePrijsInCenten;
        verspakket.AantalPersonen = SatepannetjeAantalPersonen;
        verspakket.SupermarktId = jumbo.Id;

        if (verspakket.Voedingswaarde is null)
        {
            verspakket.Voedingswaarde = CreateVoedingswaarde(verspakket.Id, now);
        }
        else
        {
            var voedingswaarde = verspakket.Voedingswaarde;
            voedingswaarde.EnergieKj = 476;
            voedingswaarde.EnergieKcal = 113;
            voedingswaarde.Vetten = 3.8m;
            voedingswaarde.WaarvanVerzadigd = 0.7m;
            voedingswaarde.Koolhydraten = 13.3m;
            voedingswaarde.WaarvanSuikers = 2.7m;
            voedingswaarde.Vezels = 1.3m;
            voedingswaarde.Eiwitten = 5.8m;
            voedingswaarde.Zout = 0.34m;
        }

        dbContext.Ingredienten.RemoveRange(
            await dbContext.Ingredienten
                .Where(i => i.VerspakketId == verspakket.Id)
                .ToListAsync());

        foreach (var ingredient in CreateIngredienten(verspakket.Id, now))
        {
            dbContext.Ingredienten.Add(ingredient);
        }

        dbContext.VerspakketAllergenen.RemoveRange(
            await dbContext.VerspakketAllergenen
                .Where(a => a.VerspakketId == verspakket.Id)
                .ToListAsync());

        foreach (var allergeen in CreateAllergenen(verspakket.Id, now))
        {
            dbContext.VerspakketAllergenen.Add(allergeen);
        }
    }

    private static Voedingswaarde CreateVoedingswaarde(Guid verspakketId, DateTime now)
    {
        return new Voedingswaarde
        {
            Id = Guid.NewGuid(),
            EnergieKj = 476,
            EnergieKcal = 113,
            Vetten = 3.8m,
            WaarvanVerzadigd = 0.7m,
            Koolhydraten = 13.3m,
            WaarvanSuikers = 2.7m,
            Vezels = 1.3m,
            Eiwitten = 5.8m,
            Zout = 0.34m,
            VerspakketId = verspakketId,
            CreatedAt = now,
            ModifiedAt = now
        };
    }

    private static List<Ingredient> CreateIngredienten(Guid verspakketId, DateTime now)
    {
        // Package contents as listed on jumbo.com; quantities are the ingredient
        // percentages applied to the 1430 g package, rounded to whole grams.
        (string Naam, decimal HoeveelheidInGram)[] ingredienten =
        [
            ("Groentemix (wortel, prei, witte kool, taugé)", 596),
            ("Satésaus", 299),
            ("Rijst", 299),
            ("Rode ui", 119),
            ("Komkommer", 100),
            ("Gebakken ui-kokos-sesammix", 19)
        ];

        return ingredienten
            .Select(i => new Ingredient
            {
                Id = Guid.NewGuid(),
                Naam = i.Naam,
                Hoeveelheid = i.HoeveelheidInGram,
                Eenheid = Eenheid.Gram,
                Inbegrepen = true,
                VerspakketId = verspakketId,
                CreatedAt = now,
                ModifiedAt = now
            })
            .ToList();
    }

    private static List<VerspakketAllergeen> CreateAllergenen(Guid verspakketId, DateTime now)
    {
        Allergeen[] allergenen = [Allergeen.Gluten, Allergeen.Melk, Allergeen.Pinda, Allergeen.Sesam];

        return allergenen
            .Select(allergeen => new VerspakketAllergeen
            {
                Id = Guid.NewGuid(),
                Allergeen = allergeen,
                VerspakketId = verspakketId,
                CreatedAt = now,
                ModifiedAt = now
            })
            .ToList();
    }
}
