using Cortex.Mediator.Queries;
using Lutra.Application.Exceptions;
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

        /// <summary>Upper bound on the free-text search term, aligned with the verspakket name length.</summary>
        public const int MaxSearchLength = 255;

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var skip = Math.Max(0, request.Skip);
            var take = Math.Clamp(request.Take, 1, MaxPageSize);
            var search = NormalizeSearch(request.Search);

            var query = context.Verspaketten
                .Where(w => w.DeletedAt == null)
                .AsNoTracking();

            if (search is not null)
            {
                // Provider-portable case-insensitive substring match on the name. SQLite-backed
                // integration tests and PostgreSQL both translate ToLower()/Contains().
                query = query.Where(v => v.Naam.ToLower().Contains(search));
            }

            // Apply sort before pagination so the database handles ordering efficiently.
            // A trailing Id tie-breaker keeps offset pages stable when sort values are equal.
            IOrderedQueryable<Domain.Entities.Verspakket> sorted = request.SortField switch
            {
                VerspakketSortField.AverageCijferSmaak =>
                    SortByScore(query, request.SortDirection, useSmaak: true),
                VerspakketSortField.AverageCijferBereiden =>
                    SortByScore(query, request.SortDirection, useSmaak: false),
                VerspakketSortField.PrijsInCenten =>
                    SortByPrice(query, request.SortDirection),
                VerspakketSortField.Naam or _ =>
                    SortByName(query, request.SortDirection),
            };

            var verspakketten = await sorted
                .ThenBy(v => v.Id)
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

        /// <summary>
        /// Trims the search term and treats empty input as "no filter". Returns a lower-cased term
        /// so the comparison is case-insensitive across providers.
        /// </summary>
        private static string? NormalizeSearch(string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return null;
            }

            var trimmed = search.Trim();
            if (trimmed.Length > MaxSearchLength)
            {
                throw new ValidationException($"De zoekterm mag maximaal {MaxSearchLength} tekens bevatten.");
            }

            return trimmed.ToLowerInvariant();
        }

        /// <summary>
        /// Orders by average score, keeping verspakketten without reviews last for both directions.
        /// </summary>
        private static IOrderedQueryable<Domain.Entities.Verspakket> SortByScore(
            IQueryable<Domain.Entities.Verspakket> query,
            SortDirection direction,
            bool useSmaak)
        {
            var ratedFirst = query.OrderBy(v => v.Beoordelingen.Any() ? 0 : 1);

            if (useSmaak)
            {
                return direction == SortDirection.Ascending
                    ? ratedFirst.ThenBy(v => v.Beoordelingen.Average(b => (double)b.CijferSmaak))
                    : ratedFirst.ThenByDescending(v => v.Beoordelingen.Average(b => (double)b.CijferSmaak));
            }

            return direction == SortDirection.Ascending
                ? ratedFirst.ThenBy(v => v.Beoordelingen.Average(b => (double)b.CijferBereiden))
                : ratedFirst.ThenByDescending(v => v.Beoordelingen.Average(b => (double)b.CijferBereiden));
        }

        /// <summary>
        /// Orders by price, keeping verspakketten without a price last for both directions.
        /// </summary>
        private static IOrderedQueryable<Domain.Entities.Verspakket> SortByPrice(
            IQueryable<Domain.Entities.Verspakket> query,
            SortDirection direction)
        {
            var pricedFirst = query.OrderBy(v => v.PrijsInCenten != null ? 0 : 1);

            return direction == SortDirection.Ascending
                ? pricedFirst.ThenBy(v => v.PrijsInCenten)
                : pricedFirst.ThenByDescending(v => v.PrijsInCenten);
        }

        private static IOrderedQueryable<Domain.Entities.Verspakket> SortByName(
            IQueryable<Domain.Entities.Verspakket> query,
            SortDirection direction) =>
            direction == SortDirection.Ascending
                ? query.OrderBy(v => v.Naam)
                : query.OrderByDescending(v => v.Naam);
    }
}
