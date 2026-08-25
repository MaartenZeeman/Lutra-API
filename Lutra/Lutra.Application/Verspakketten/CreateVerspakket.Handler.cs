using Cortex.Mediator.Commands;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Verspakketten;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten;

public sealed partial class CreateVerspakket
{
    public sealed class Handler(ILutraDbContext context) : ICommandHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var supermarktExists = await context.Supermarkten
                .AsNoTracking()
                .AnyAsync(s => s.Id == request.SupermarktId, cancellationToken);

            if (!supermarktExists)
            {
                throw new InvalidOperationException($"Supermarkt with id '{request.SupermarktId}' was not found.");
            }

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

            var now = DateTime.UtcNow;
            var verspakket = new Domain.Entities.Verspakket
            {
                Id = Guid.NewGuid(),
                Naam = request.Naam,
                PrijsInCenten = request.PrijsInCenten,
                AantalPersonen = request.AantalPersonen,
                SupermarktId = request.SupermarktId,
                CreatedAt = now,
                ModifiedAt = now
            };

            if (request.Beoordeling is not null)
            {
                verspakket.AddBeoordeling(new Domain.Entities.Beoordeling
                {
                    Id = Guid.NewGuid(),
                    CijferSmaak = request.Beoordeling.CijferSmaak,
                    CijferBereiden = request.Beoordeling.CijferBereiden,
                    Aanbevolen = request.Beoordeling.Aanbevolen,
                    Tekst = request.Beoordeling.Tekst,
                    VerspakketId = verspakket.Id,
                    CreatedAt = now,
                    ModifiedAt = now
                });
            }

            if (request.Fotos is { Count: > 0 })
            {
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

            if (request.Ingredienten is { Count: > 0 })
            {
                foreach (var ingredient in request.Ingredienten)
                {
                    verspakket.AddIngredient(new Domain.Entities.Ingredient
                    {
                        Id = Guid.NewGuid(),
                        Naam = ingredient.Naam,
                        Hoeveelheid = ingredient.Hoeveelheid,
                        Eenheid = ingredient.Eenheid,
                        Inbegrepen = ingredient.Inbegrepen,
                        VerspakketId = verspakket.Id,
                        CreatedAt = now,
                        ModifiedAt = now
                    });
                }
            }

            await context.Verspaketten.AddAsync(verspakket, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new Response { Id = verspakket.Id };
        }
    }
}
