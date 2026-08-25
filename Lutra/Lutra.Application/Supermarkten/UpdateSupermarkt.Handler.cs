using Cortex.Mediator.Commands;
using Lutra.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Supermarkten;

public sealed partial class UpdateSupermarkt
{
    public sealed class Handler(ILutraDbContext context) : ICommandHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Naam))
                throw new ArgumentException("Naam mag niet leeg zijn.", nameof(request.Naam));

            if (request.Naam.Length > 50)
                throw new ArgumentException("Naam mag maximaal 50 tekens bevatten.", nameof(request.Naam));

            var supermarkt = await context.Supermarkten
                .FirstOrDefaultAsync(s => s.Id == request.Id && s.DeletedAt == null, cancellationToken);

            if (supermarkt is null)
            {
                throw new InvalidOperationException($"Supermarkt with id '{request.Id}' was not found.");
            }

            supermarkt.Naam = request.Naam;
            supermarkt.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return new Response();
        }
    }
}