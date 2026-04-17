using Cortex.Mediator.Queries;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Supermarkten;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Supermarkten
{
    public sealed partial class GetSupermarkten
    {
        public sealed class Handler(ILutraDbContext context) : IQueryHandler<Query, Response>
        {
            public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
            {
                var supermarkten = await context.Supermarkten
                    .AsNoTracking()
                    .Where(w => w.DeletedAt == null)
                    .OrderBy(s => s.Naam)
                    .Skip(request.Skip)
                    .Take(request.Take)
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
}
