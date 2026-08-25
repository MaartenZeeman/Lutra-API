using Cortex.Mediator.Commands;
using Lutra.Application.Interfaces;

namespace Lutra.Application.Supermarkten;

public sealed partial class CreateSupermarkt
{
    public sealed class Handler(ILutraDbContext context) : ICommandHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Naam))
                throw new ArgumentException("Naam mag niet leeg zijn.", nameof(request.Naam));

            if (request.Naam.Length > 50)
                throw new ArgumentException("Naam mag maximaal 50 tekens bevatten.", nameof(request.Naam));

            var now = DateTime.UtcNow;
            var supermarkt = new Domain.Entities.Supermarkt
            {
                Id = Guid.NewGuid(),
                Naam = request.Naam,
                CreatedAt = now,
                ModifiedAt = now
            };

            context.Supermarkten.Add(supermarkt);
            await context.SaveChangesAsync(cancellationToken);

            return new Response { Id = supermarkt.Id };
        }
    }
}