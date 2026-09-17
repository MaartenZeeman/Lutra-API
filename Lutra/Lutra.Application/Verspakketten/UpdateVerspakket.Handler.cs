using Cortex.Mediator.Commands;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Verspakketten;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten;

public sealed partial class UpdateVerspakket
{
    /// <summary>
    /// Handles update requests for verspakketten.
    /// </summary>
    public sealed class Handler(ILutraDbContext context) : ICommandHandler<Command, Response>
    {
        /// <summary>
        /// Updates an existing verspakket.
        /// </summary>
        /// <param name="request">The update command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An empty response.</returns>
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Naam))
                throw new ValidationException("Naam mag niet leeg zijn.");

            if (request.Naam.Length > 255)
                throw new ValidationException("Naam mag maximaal 255 tekens bevatten.");

            if (request.PrijsInCenten < 0)
                throw new ValidationException("PrijsInCenten mag niet negatief zijn.");

            if (request.AantalPersonen is < 1 or > 10)
                throw new ValidationException("AantalPersonen moet tussen 1 en 10 liggen.");

            if (request.Ingredienten is not null)
            {
                foreach (var ingredient in request.Ingredienten)
                {
                    if (string.IsNullOrWhiteSpace(ingredient.Naam))
                        throw new ValidationException("Ingrediëntnaam mag niet leeg zijn.");

                    if (ingredient.Naam.Length > 100)
                        throw new ValidationException("Ingrediëntnaam mag maximaal 100 tekens bevatten.");

                    if (ingredient.Hoeveelheid <= 0)
                        throw new ValidationException("Hoeveelheid moet groter zijn dan 0.");

                    if (!Enum.IsDefined(ingredient.Eenheid))
                        throw new ValidationException("Eenheid is geen geldige waarde.");
                }
            }

            if (request.Voedingswaarden is not null)
            {
                ValidateVoedingswaarden(request.Voedingswaarden);
            }

            if (request.Allergenen is not null)
            {
                foreach (var allergeen in request.Allergenen)
                {
                    if (!Enum.IsDefined(allergeen))
                        throw new ValidationException("Allergeen is geen geldige waarde.");
                }

                if (request.Allergenen.Count != request.Allergenen.Distinct().Count())
                    throw new ValidationException("Allergenen mogen niet dubbel voorkomen.");
            }

            var decodedFotos = VerspakketFotoValidator.DecodeAll(request.Fotos);

            var verspakket = await context.Verspaketten
                .Include(v => v.Fotos)
                .Include(v => v.Ingredienten)
                .Include(v => v.Voedingswaarden)
                .FirstOrDefaultAsync(v => v.Id == request.Id && v.DeletedAt == null, cancellationToken);

            if (verspakket is null)
            {
                throw new NotFoundException($"Verspakket with id '{request.Id}' was not found.");
            }

            var supermarktExists = await context.Supermarkten
                .AsNoTracking()
                .AnyAsync(s => s.Id == request.SupermarktId && s.DeletedAt == null, cancellationToken);

            if (!supermarktExists)
            {
                throw new NotFoundException($"Supermarkt with id '{request.SupermarktId}' was not found.");
            }

            verspakket.Naam = request.Naam;
            verspakket.PrijsInCenten = request.PrijsInCenten;
            verspakket.AantalPersonen = request.AantalPersonen;
            verspakket.SupermarktId = request.SupermarktId;
            verspakket.ModifiedAt = DateTime.UtcNow;

            if (request.Fotos is not null)
            {
                // Replace all existing fotos
                foreach (var existing in verspakket.Fotos.ToList())
                    verspakket.RemoveFoto(existing.Id);

                context.VerspakketFotos.RemoveRange(
                    await context.VerspakketFotos
                        .Where(f => f.VerspakketId == request.Id)
                        .ToListAsync(cancellationToken));

                var now = DateTime.UtcNow;
                foreach (var foto in decodedFotos)
                {
                    verspakket.AddFoto(new Domain.Entities.VerspakketFoto
                    {
                        Id = Guid.NewGuid(),
                        Data = foto.Data,
                        IsMainImage = foto.IsMainImage,
                        VerspakketId = verspakket.Id,
                        CreatedAt = now,
                        ModifiedAt = now
                    });
                }
            }

            if (request.Ingredienten is not null)
            {
                // Replace all existing ingredients
                context.Ingredienten.RemoveRange(
                    await context.Ingredienten
                        .Where(i => i.VerspakketId == request.Id)
                        .ToListAsync(cancellationToken));

                var now = DateTime.UtcNow;
                var ingredienten = request.Ingredienten
                    .Select(ing => new Domain.Entities.Ingredient
                    {
                        Id = Guid.NewGuid(),
                        Naam = ing.Naam,
                        Hoeveelheid = ing.Hoeveelheid,
                        Eenheid = ing.Eenheid,
                        Inbegrepen = ing.Inbegrepen,
                        VerspakketId = verspakket.Id,
                        CreatedAt = now,
                        ModifiedAt = now
                    })
                    .ToList();

                context.Ingredienten.AddRange(ingredienten);
            }

            if (request.Voedingswaarden is not null)
            {
                // Replace all existing voedingswaarden
                context.Voedingswaarden.RemoveRange(
                    await context.Voedingswaarden
                        .Where(w => w.VerspakketId == request.Id)
                        .ToListAsync(cancellationToken));

                var now = DateTime.UtcNow;
                var voedingswaarden = request.Voedingswaarden
                    .Select(w => new Domain.Entities.Voedingswaarde
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
                        VerspakketId = verspakket.Id,
                        CreatedAt = now,
                        ModifiedAt = now
                    })
                    .ToList();

                context.Voedingswaarden.AddRange(voedingswaarden);
            }

            if (request.Allergenen is not null)
            {
                // Replace all existing allergenen
                context.VerspakketAllergenen.RemoveRange(
                    await context.VerspakketAllergenen
                        .Where(a => a.VerspakketId == request.Id)
                        .ToListAsync(cancellationToken));

                var now = DateTime.UtcNow;
                var allergenen = request.Allergenen
                    .Select(a => new Domain.Entities.VerspakketAllergeen
                    {
                        Id = Guid.NewGuid(),
                        Allergeen = a,
                        VerspakketId = verspakket.Id,
                        CreatedAt = now,
                        ModifiedAt = now
                    })
                    .ToList();

                context.VerspakketAllergenen.AddRange(allergenen);
            }

            await context.SaveChangesAsync(cancellationToken);

            return new Response();
        }

        private static void ValidateVoedingswaarden(IReadOnlyList<Voedingswaarde> voedingswaarden)
        {
            if (voedingswaarden.Count > 2)
                throw new ValidationException("Een verspakket mag maximaal twee voedingswaarden hebben (per 100 gram en per portie).");

            if (voedingswaarden.Select(w => w.Basis).Distinct().Count() != voedingswaarden.Count)
                throw new ValidationException("Voedingswaarden mogen per basis maar één keer voorkomen.");

            foreach (var voedingswaarde in voedingswaarden)
            {
                if (!Enum.IsDefined(voedingswaarde.Basis))
                    throw new ValidationException("Voedingswaardebasis is geen geldige waarde.");

                ValidateVoedingswaarde(voedingswaarde);
            }
        }

        private static void ValidateVoedingswaarde(Voedingswaarde voedingswaarde)
        {
            if (voedingswaarde.EnergieKj is < 0)
                throw new ValidationException("EnergieKj mag niet negatief zijn.");

            if (voedingswaarde.EnergieKcal is < 0)
                throw new ValidationException("EnergieKcal mag niet negatief zijn.");

            if (voedingswaarde.Vetten is < 0)
                throw new ValidationException("Vetten mag niet negatief zijn.");

            if (voedingswaarde.WaarvanVerzadigd is < 0)
                throw new ValidationException("WaarvanVerzadigd mag niet negatief zijn.");

            if (voedingswaarde.Koolhydraten is < 0)
                throw new ValidationException("Koolhydraten mag niet negatief zijn.");

            if (voedingswaarde.WaarvanSuikers is < 0)
                throw new ValidationException("WaarvanSuikers mag niet negatief zijn.");

            if (voedingswaarde.Vezels is < 0)
                throw new ValidationException("Vezels mag niet negatief zijn.");

            if (voedingswaarde.Eiwitten is < 0)
                throw new ValidationException("Eiwitten mag niet negatief zijn.");

            if (voedingswaarde.Zout is < 0)
                throw new ValidationException("Zout mag niet negatief zijn.");

            if (voedingswaarde.WaarvanVerzadigd > voedingswaarde.Vetten)
                throw new ValidationException("WaarvanVerzadigd mag niet groter zijn dan Vetten.");

            if (voedingswaarde.WaarvanSuikers > voedingswaarde.Koolhydraten)
                throw new ValidationException("WaarvanSuikers mag niet groter zijn dan Koolhydraten.");
        }
    }
}
