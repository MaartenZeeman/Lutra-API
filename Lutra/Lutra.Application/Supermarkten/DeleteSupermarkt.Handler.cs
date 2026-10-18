using Cortex.Mediator.Commands;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Supermarkten;

public sealed partial class DeleteSupermarkt
{
    public sealed class Handler(ILutraDbContext context) : ICommandHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var supermarkt = await context.Supermarkten
                .FirstOrDefaultAsync(s => s.Id == request.Id && s.DeletedAt == null, cancellationToken);

            if (supermarkt is null)
            {
                throw new NotFoundException($"Supermarkt with id '{request.Id}' was not found.");
            }

            // Removing a supermarkt must not silently take its verspakketten with it.
            var hasVerspakketten = await context.Verspaketten
                .AnyAsync(v => v.SupermarktId == request.Id, cancellationToken);

            if (hasVerspakketten)
            {
                throw new ConflictException($"Supermarkt with id '{request.Id}' still has verspakketten and cannot be removed.");
            }

            supermarkt.DeletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return new Response();
        }
    }
}
