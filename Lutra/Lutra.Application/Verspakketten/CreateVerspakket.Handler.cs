using Cortex.Mediator.Commands;
using Lutra.Application.Exceptions;
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
                throw new NotFoundException($"Supermarkt with id '{request.SupermarktId}' was not found.");
            }

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

            var now = DateTime.UtcNow;
            var verspakket = new Domain.Entities.Verspakket
            {
                Id = Guid.NewGuid(),
                Naam = request.Naam,
                PrijsInCenten = request.PrijsInCenten,
                AantalPersonen = request.AantalPersonen,
                BronUrl = request.BronUrl,
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

            if (decodedFotos.Count > 0)
            {
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

            if (request.Voedingswaarden is { Count: > 0 })
            {
                foreach (var voedingswaarde in request.Voedingswaarden)
                {
                    verspakket.AddVoedingswaarde(new Domain.Entities.Voedingswaarde
                    {
                        Id = Guid.NewGuid(),
                        Basis = voedingswaarde.Basis,
                        EnergieKj = voedingswaarde.EnergieKj,
                        EnergieKcal = voedingswaarde.EnergieKcal,
                        Vetten = voedingswaarde.Vetten,
                        WaarvanVerzadigd = voedingswaarde.WaarvanVerzadigd,
                        Koolhydraten = voedingswaarde.Koolhydraten,
                        WaarvanSuikers = voedingswaarde.WaarvanSuikers,
                        Vezels = voedingswaarde.Vezels,
                        Eiwitten = voedingswaarde.Eiwitten,
                        Zout = voedingswaarde.Zout,
                        VerspakketId = verspakket.Id,
                        CreatedAt = now,
                        ModifiedAt = now
                    });
                }
            }

            if (request.Allergenen is { Count: > 0 })
            {
                foreach (var allergeen in request.Allergenen)
                {
                    verspakket.AddAllergeen(new Domain.Entities.VerspakketAllergeen
                    {
                        Id = Guid.NewGuid(),
                        Allergeen = allergeen,
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
