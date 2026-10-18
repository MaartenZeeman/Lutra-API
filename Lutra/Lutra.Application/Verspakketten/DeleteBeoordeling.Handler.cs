using Cortex.Mediator.Commands;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten;

public sealed partial class DeleteBeoordeling
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

            beoordeling.DeletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return new Response();
        }
    }
}
