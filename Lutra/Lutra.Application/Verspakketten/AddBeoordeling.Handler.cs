using Cortex.Mediator.Commands;
using Lutra.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten;

public sealed partial class AddBeoordeling
{
    public sealed class Handler(ILutraDbContext context) : ICommandHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var verspakketExists = await context.Verspaketten
                .AsNoTracking()
                .AnyAsync(v => v.Id == request.VerspakketId && v.DeletedAt == null, cancellationToken);

            if (!verspakketExists)
            {
                throw new InvalidOperationException($"Verspakket with id '{request.VerspakketId}' was not found.");
            }

            var now = DateTime.UtcNow;
            var beoordeling = new Domain.Entities.Beoordeling
            {
                Id = Guid.NewGuid(),
                CijferSmaak = request.CijferSmaak,
                CijferBereiden = request.CijferBereiden,
                Aanbevolen = request.Aanbevolen,
                Tekst = request.Tekst,
                VerspakketId = request.VerspakketId,
                CreatedAt = now,
                ModifiedAt = now
            };

            await context.Beoordelingen.AddAsync(beoordeling, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new Response { Id = beoordeling.Id };
        }
    }
}