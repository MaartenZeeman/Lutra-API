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
                var query = context.Verspaketten.AsNoTracking();

                // Apply sort before pagination so the database handles ordering efficiently.
                IOrderedQueryable<Domain.Entities.Verspakket> sorted = request.SortField switch
                {
                    VerspakketSortField.AverageCijferSmaak =>
                        request.SortDirection == SortDirection.Ascending
                            ? query.OrderBy(v => v.Beoordelingen.Average(b => (double)b.CijferSmaak))
                            : query.OrderByDescending(v => v.Beoordelingen.Average(b => (double)b.CijferSmaak)),
                    VerspakketSortField.AverageCijferBereiden =>
                        request.SortDirection == SortDirection.Ascending
                            ? query.OrderBy(v => v.Beoordelingen.Average(b => (double)b.CijferBereiden))
                            : query.OrderByDescending(v => v.Beoordelingen.Average(b => (double)b.CijferBereiden)),
                    VerspakketSortField.PrijsInCenten =>
                        request.SortDirection == SortDirection.Ascending
                            ? query.OrderBy(v => v.PrijsInCenten)
                            : query.OrderByDescending(v => v.PrijsInCenten),
                    VerspakketSortField.Naam or _ =>
                        request.SortDirection == SortDirection.Ascending
                            ? query.OrderBy(v => v.Naam)
                            : query.OrderByDescending(v => v.Naam),
                };

                var verspakketten = await sorted
                    .Skip(request.Skip)
                    .Take(request.Take)
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
                    .ToListAsync(cancellationToken);

                return new Response { Verspakketten = verspakketten };
            }
        }
    }
}
