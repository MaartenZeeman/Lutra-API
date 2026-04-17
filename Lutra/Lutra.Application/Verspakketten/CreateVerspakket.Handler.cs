using Cortex.Mediator.Commands;
using Lutra.Application.Interfaces;
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

            await context.Verspaketten.AddAsync(verspakket, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new Response { Id = verspakket.Id };
        }
    }
}
