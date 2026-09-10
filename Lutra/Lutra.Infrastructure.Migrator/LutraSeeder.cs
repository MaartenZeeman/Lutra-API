using Lutra.Domain.Entities;
using Lutra.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Infrastructure.Migrator;

/// <summary>
/// Seeds the database with the known supermarkten and the Jumbo Satépannetje verspakket.
/// Reconciles on every run: existing seeded entities are re-applied (scalars, voedingswaarden,
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

            foreach (var voedingswaarde in CreateVoedingswaarden(verspakket.Id, now))
            {
                verspakket.AddVoedingswaarde(voedingswaarde);
            }

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

        dbContext.Voedingswaarden.RemoveRange(
            await dbContext.Voedingswaarden
                .Where(w => w.VerspakketId == verspakket.Id)
                .ToListAsync());

        foreach (var voedingswaarde in CreateVoedingswaarden(verspakket.Id, now))
        {
            dbContext.Voedingswaarden.Add(voedingswaarde);
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

    private static List<Voedingswaarde> CreateVoedingswaarden(Guid verspakketId, DateTime now)
    {
        // Nutrition label of the Jumbo Satépannetje, both as printed on the packaging:
        // per 100 g (bereid product) and per portie (572 g bereid product).
        (VoedingswaardeBasis Basis, decimal EnergieKj, decimal EnergieKcal, decimal Vetten, decimal WaarvanVerzadigd,
            decimal Koolhydraten, decimal WaarvanSuikers, decimal Vezels, decimal Eiwitten, decimal Zout)[] waarden =
        [
            (VoedingswaardeBasis.Per100Gram, 476, 113, 3.8m, 0.7m, 13.3m, 2.7m, 1.3m, 5.8m, 0.34m),
            (VoedingswaardeBasis.PerPortie, 2723, 648, 21.9m, 3.8m, 76.0m, 15.4m, 7.5m, 33.1m, 1.94m)
        ];

        return waarden
            .Select(w => new Voedingswaarde
            {
                Id = Guid.NewGuid(),
                Basis = w.Basis,
                EnergieKj = w.EnergieKj,
                EnergieKcal = w.EnergieKcal,
                Vetten = w.Vetten,
                WaarvanVerzadigd = w.WaarvanVerzadigd,
                Koolhydraten = w.Koolhydraten,
                WaarvanSuikers = w.WaarvanSuikers,
                Vezels = w.Vezels,
                Eiwitten = w.Eiwitten,
                Zout = w.Zout,
                VerspakketId = verspakketId,
                CreatedAt = now,
                ModifiedAt = now
            })
            .ToList();
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
