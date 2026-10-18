using Cortex.Mediator.Commands;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten;

public sealed partial class UpdateBeoordeling
{
    public sealed class Handler(ILutraDbContext context) : ICommandHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var beoordeling = await context.Beoordelingen
                .FirstOrDefaultAsync(
                    b => b.Id == request.BeoordelingId
                        && b.VerspakketId == request.VerspakketId
                        && b.DeletedAt == null,
                    cancellationToken);

            if (beoordeling is null)
            {
                throw new NotFoundException(
                    $"Beoordeling with id '{request.BeoordelingId}' was not found for verspakket '{request.VerspakketId}'.");
            }

            beoordeling.CijferSmaak = request.CijferSmaak;
            beoordeling.CijferBereiden = request.CijferBereiden;
            beoordeling.Aanbevolen = request.Aanbevolen;
            beoordeling.Tekst = request.Tekst;
            beoordeling.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return new Response();
        }
    }
}
