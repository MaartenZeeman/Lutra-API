using Cortex.Mediator.Commands;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten;

public sealed partial class DeleteVerspakket
{
    public sealed class Handler(ILutraDbContext context) : ICommandHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var verspakket = await context.Verspaketten
                .Include(v => v.Beoordelingen)
                .Include(v => v.Fotos)
                .Include(v => v.Ingredienten)
                .Include(v => v.Voedingswaarden)
                .Include(v => v.Allergenen)
                .FirstOrDefaultAsync(v => v.Id == request.Id && v.DeletedAt == null, cancellationToken);

            if (verspakket is null)
            {
                throw new NotFoundException($"Verspakket with id '{request.Id}' was not found.");
            }

            var now = DateTime.UtcNow;

            verspakket.DeletedAt = now;

            // The unique index on BronUrl is not filtered on DeletedAt, so clear it to let the
            // normalized source URL be imported again after a removal.
            verspakket.BronUrl = null;

            // Query filters do not cascade, so every child row is soft-deleted explicitly.
            foreach (var beoordeling in verspakket.Beoordelingen)
                beoordeling.DeletedAt = now;

            foreach (var foto in verspakket.Fotos)
                foto.DeletedAt = now;

            foreach (var ingredient in verspakket.Ingredienten)
                ingredient.DeletedAt = now;

            foreach (var voedingswaarde in verspakket.Voedingswaarden)
                voedingswaarde.DeletedAt = now;

            foreach (var allergeen in verspakket.Allergenen)
                allergeen.DeletedAt = now;

            await context.SaveChangesAsync(cancellationToken);

            return new Response();
        }
    }
}
