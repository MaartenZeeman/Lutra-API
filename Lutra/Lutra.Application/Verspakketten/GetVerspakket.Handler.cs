using Cortex.Mediator.Queries;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Supermarkten;
using Lutra.Application.Models.Verspakketten;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten;

    public sealed partial class GetVerspakket
    {
        public sealed class Handler(ILutraDbContext context) : IQueryHandler<Query, Response?>
        {
            public async Task<Response?> Handle(Query request, CancellationToken cancellationToken)
            {
                            var verspakket = await context.Verspaketten
                                .AsNoTracking()
                                .Where(v => v.Id == request.Id)
                                .Select(v => new Verspakket
                                {
                                    Id = v.Id,
                                    Naam = v.Naam,
                                    PrijsInCenten = v.PrijsInCenten,
                                    AantalPersonen = v.AantalPersonen,
                                    AverageCijferSmaak = v.Beoordelingen.Any() ? v.Beoordelingen.Average(b => (double)b.CijferSmaak) : null,
                                    AverageCijferBereiden = v.Beoordelingen.Any() ? v.Beoordelingen.Average(b => (double)b.CijferBereiden) : null,
                                    Beoordelingen = v.Beoordelingen
                                        .Select(b => new Beoordeling
                                        {
                                            CijferSmaak = b.CijferSmaak,
                                            CijferBereiden = b.CijferBereiden,
                                            Aanbevolen = b.Aanbevolen,
                                            Tekst = b.Tekst
                                        })
                                        .ToArray(),
                                    Supermarkt = new Supermarkt
                                    {
                                        Id = v.Supermarkt.Id,
                                        Naam = v.Supermarkt.Naam
                                    },
                                    Fotos = v.Fotos
                                        .Select(f => new VerspakketFotoResponse(
                                            f.Id,
                                            Convert.ToBase64String(f.Data),
                                            f.IsMainImage))
                                        .ToArray(),
                                    Ingredienten = v.Ingredienten
                                        .Select(i => new Ingredient(
                                            i.Naam,
                                            i.Hoeveelheid,
                                            i.Eenheid,
                                            i.Inbegrepen))
                                        .ToArray()
                                })
                                .SingleOrDefaultAsync(cancellationToken);

                            return verspakket is null ? null : new Response { Verspakket = verspakket };
                        }
                    }
                }
