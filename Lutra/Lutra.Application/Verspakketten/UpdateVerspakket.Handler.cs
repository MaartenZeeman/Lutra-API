using Cortex.Mediator.Commands;
using Lutra.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten;

public sealed partial class UpdateVerspakket
{
    /// <summary>
    /// Handles update requests for verspakketten.
    /// </summary>
    public sealed class Handler(ILutraDbContext context) : ICommandHandler<Command, Response>
    {
        /// <summary>
        /// Updates an existing verspakket.
        /// </summary>
        /// <param name="request">The update command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An empty response.</returns>
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Naam))
                throw new ArgumentException("Naam mag niet leeg zijn.", nameof(request.Naam));

            if (request.Naam.Length > 50)
                throw new ArgumentException("Naam mag maximaal 50 tekens bevatten.", nameof(request.Naam));

            if (request.PrijsInCenten < 0)
                throw new ArgumentException("PrijsInCenten mag niet negatief zijn.", nameof(request.PrijsInCenten));

            if (request.AantalPersonen is < 1 or > 10)
                throw new ArgumentException("AantalPersonen moet tussen 1 en 10 liggen.", nameof(request.AantalPersonen));

            var verspakket = await context.Verspaketten
                .FirstOrDefaultAsync(v => v.Id == request.Id && v.DeletedAt == null, cancellationToken);

            if (verspakket is null)
            {
                throw new InvalidOperationException($"Verspakket with id '{request.Id}' was not found.");
            }

            var supermarktExists = await context.Supermarkten
                .AsNoTracking()
                .AnyAsync(s => s.Id == request.SupermarktId && s.DeletedAt == null, cancellationToken);

            if (!supermarktExists)
            {
                throw new InvalidOperationException($"Supermarkt with id '{request.SupermarktId}' was not found.");
            }

            verspakket.Naam = request.Naam;
            verspakket.PrijsInCenten = request.PrijsInCenten;
            verspakket.AantalPersonen = request.AantalPersonen;
            verspakket.SupermarktId = request.SupermarktId;
            verspakket.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return new Response();
        }
    }
}
