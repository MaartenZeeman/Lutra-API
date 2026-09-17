using Cortex.Mediator.Queries;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Supermarkten;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Supermarkten;

public sealed partial class GetSupermarkten
{
    public sealed class Handler(ILutraDbContext context) : IQueryHandler<Query, Response>
    {
        /// <summary>Upper bound on page size so a client cannot force the API to load the whole table.</summary>
        public const int MaxPageSize = 200;

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var skip = Math.Max(0, request.Skip);
            var take = Math.Clamp(request.Take, 1, MaxPageSize);

            var supermarkten = await context.Supermarkten
                .AsNoTracking()
                .Where(w => w.DeletedAt == null)
                .OrderBy(s => s.Naam)
                .Skip(skip)
                .Take(take)
                .Select(s => new Supermarkt
                {
                    Id = s.Id,
                    Naam = s.Naam
                })
                .ToListAsync(cancellationToken);

            return new Response { Supermarkten = supermarkten };
        }
    }
}
