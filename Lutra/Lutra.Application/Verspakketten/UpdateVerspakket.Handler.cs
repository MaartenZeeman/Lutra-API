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

            if (request.Naam.Length > 50)
                throw new ValidationException("Naam mag maximaal 50 tekens bevatten.");

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

            if (request.Voedingswaarde is not null)
            {
                ValidateVoedingswaarde(request.Voedingswaarde);
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

            var verspakket = await context.Verspaketten
                .Include(v => v.Fotos)
                .Include(v => v.Ingredienten)
                .Include(v => v.Voedingswaarde)
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
                foreach (var foto in request.Fotos)
                {
                    verspakket.AddFoto(new Domain.Entities.VerspakketFoto
                    {
                        Id = Guid.NewGuid(),
                        Data = Convert.FromBase64String(foto.Base64Data),
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

            if (request.Voedingswaarde is not null)
            {
                if (verspakket.Voedingswaarde is null)
                {
                    verspakket.Voedingswaarde = new Domain.Entities.Voedingswaarde
                    {
                        Id = Guid.NewGuid(),
                        EnergieKj = request.Voedingswaarde.EnergieKj,
                        EnergieKcal = request.Voedingswaarde.EnergieKcal,
                        Vetten = request.Voedingswaarde.Vetten,
                        WaarvanVerzadigd = request.Voedingswaarde.WaarvanVerzadigd,
                        Koolhydraten = request.Voedingswaarde.Koolhydraten,
                        WaarvanSuikers = request.Voedingswaarde.WaarvanSuikers,
                        Vezels = request.Voedingswaarde.Vezels,
                        Eiwitten = request.Voedingswaarde.Eiwitten,
                        Zout = request.Voedingswaarde.Zout,
                        VerspakketId = verspakket.Id,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    var voedingswaarde = verspakket.Voedingswaarde;
                    voedingswaarde.EnergieKj = request.Voedingswaarde.EnergieKj;
                    voedingswaarde.EnergieKcal = request.Voedingswaarde.EnergieKcal;
                    voedingswaarde.Vetten = request.Voedingswaarde.Vetten;
                    voedingswaarde.WaarvanVerzadigd = request.Voedingswaarde.WaarvanVerzadigd;
                    voedingswaarde.Koolhydraten = request.Voedingswaarde.Koolhydraten;
                    voedingswaarde.WaarvanSuikers = request.Voedingswaarde.WaarvanSuikers;
                    voedingswaarde.Vezels = request.Voedingswaarde.Vezels;
                    voedingswaarde.Eiwitten = request.Voedingswaarde.Eiwitten;
                    voedingswaarde.Zout = request.Voedingswaarde.Zout;
                }
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
