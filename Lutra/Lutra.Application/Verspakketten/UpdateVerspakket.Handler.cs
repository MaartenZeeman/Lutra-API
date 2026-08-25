using Cortex.Mediator.Commands;
using Lutra.Application.Interfaces;
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
                throw new ArgumentException("Naam mag niet leeg zijn.", nameof(request.Naam));

            if (request.Naam.Length > 50)
                throw new ArgumentException("Naam mag maximaal 50 tekens bevatten.", nameof(request.Naam));

            if (request.PrijsInCenten < 0)
                throw new ArgumentException("PrijsInCenten mag niet negatief zijn.", nameof(request.PrijsInCenten));

            if (request.AantalPersonen is < 1 or > 10)
                throw new ArgumentException("AantalPersonen moet tussen 1 en 10 liggen.", nameof(request.AantalPersonen));

            if (request.Ingredienten is not null)
            {
                foreach (var ingredient in request.Ingredienten)
                {
                    if (string.IsNullOrWhiteSpace(ingredient.Naam))
                        throw new ArgumentException("Ingrediëntnaam mag niet leeg zijn.", nameof(request.Ingredienten));

                    if (ingredient.Naam.Length > 100)
                        throw new ArgumentException("Ingrediëntnaam mag maximaal 100 tekens bevatten.", nameof(request.Ingredienten));

                    if (ingredient.Hoeveelheid <= 0)
                        throw new ArgumentException("Hoeveelheid moet groter zijn dan 0.", nameof(request.Ingredienten));

                    if (!Enum.IsDefined(ingredient.Eenheid))
                        throw new ArgumentException("Eenheid is geen geldige waarde.", nameof(request.Ingredienten));
                }
            }

            var verspakket = await context.Verspaketten
                .Include(v => v.Fotos)
                .Include(v => v.Ingredienten)
                .FirstOrDefaultAsync(v => v.Id == request.Id && v.DeletedAt == null, cancellationToken);

            if (verspakket is null)
            {
                throw new InvalidOperationException($"Verspakket with id '{request.Id}' was not found.");
            }

            var supermarktExists = await context.Supermarkten
                .AsNoTracking()
                .AnyAsync(s => s.Id == request.SupermarktId && s.DeletedAt == null, cancellationToken);

            if (!supermarktExists)
            {
                throw new InvalidOperationException($"Supermarkt with id '{request.SupermarktId}' was not found.");
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

            await context.SaveChangesAsync(cancellationToken);

            return new Response();
        }
    }
}
