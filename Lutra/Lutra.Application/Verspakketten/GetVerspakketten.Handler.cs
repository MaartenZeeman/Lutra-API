using Cortex.Mediator.Queries;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Supermarkten;
using Lutra.Application.Models.Verspakketten;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten
{
    public sealed partial class GetVerspakketten
    {
        public sealed class Handler(ILutraDbContext context) : IQueryHandler<Query, Response>
        {
            public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
            {
                var verspakketten = await context.Verspaketten
                    .AsNoTracking()
                    .OrderBy(v => v.Naam)
                    .Skip(request.Skip)
                    .Take(request.Take)
                    .Select(v => new Verspakket
                    {
                        Naam = v.Naam,
                        PrijsInCenten = null,
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
                            Naam = v.Supermarkt.Naam
                        }
                    })
                    .ToListAsync(cancellationToken);

                return new Response { Verspakketten = verspakketten };
            }
        }
    }
}
