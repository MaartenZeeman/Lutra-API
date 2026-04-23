using Cortex.Mediator.Commands;

namespace Lutra.Application.Verspakketten;

public sealed partial class UpdateVerspakket
{
    /// <summary>
    /// Updates an existing verspakket.
    /// </summary>
    public sealed record Command(Guid Id, string Naam, int PrijsInCenten, int AantalPersonen, Guid SupermarktId) : ICommand<Response>;
}
