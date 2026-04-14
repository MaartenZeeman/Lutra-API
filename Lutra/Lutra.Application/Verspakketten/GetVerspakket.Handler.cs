using Cortex.Mediator.Queries;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Supermarkten;
using Lutra.Application.Models.Verspakketten;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten
{
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
                        Naam = v.Naam,
                        PrijsInCenten = v.PrijsInCenten,
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
                    .SingleOrDefaultAsync(cancellationToken);

                return verspakket is null ? null : new Response { Verspakket = verspakket };
            }
        }
    }
}
