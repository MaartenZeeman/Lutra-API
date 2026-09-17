using Cortex.Mediator.Queries;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Supermarkten;
using Lutra.Application.Models.Verspakketten;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten;

public sealed partial class GetVerspakketten
{
    public sealed class Handler(ILutraDbContext context) : IQueryHandler<Query, Response>
    {
        /// <summary>Upper bound on page size so a client cannot force the API to load the whole table.</summary>
        public const int MaxPageSize = 200;

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var skip = Math.Max(0, request.Skip);
            var take = Math.Clamp(request.Take, 1, MaxPageSize);

            var query = context.Verspaketten
                .Where(w => w.DeletedAt == null)
                .AsNoTracking();

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
                .Skip(skip)
                .Take(take)
                .Select(v => new VerspakketSummary
                {
                    Id = v.Id,
                    Naam = v.Naam,
                    PrijsInCenten = v.PrijsInCenten,
                    AantalPersonen = v.AantalPersonen,
                    AverageCijferSmaak = v.Beoordelingen.Any() ? v.Beoordelingen.Average(b => (double)b.CijferSmaak) : null,
                    AverageCijferBereiden = v.Beoordelingen.Any() ? v.Beoordelingen.Average(b => (double)b.CijferBereiden) : null,
                    Supermarkt = new Supermarkt
                    {
                        Id = v.Supermarkt.Id,
                        Naam = v.Supermarkt.Naam
                    },
                    Foto = v.Fotos
                        .Where(f => f.IsMainImage)
                        .Select(f => new VerspakketFotoResponse(
                            f.Id,
                            Convert.ToBase64String(f.Data),
                            f.IsMainImage))
                        .SingleOrDefault()
                })
                .ToListAsync(cancellationToken);

            return new Response { Verspakketten = verspakketten };
        }
    }
}
