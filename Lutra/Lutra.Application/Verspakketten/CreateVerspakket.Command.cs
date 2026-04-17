using Cortex.Mediator.Commands;

namespace Lutra.Application.Verspakketten;

public sealed partial class CreateVerspakket
{
    public sealed record Command(
        string Naam,
        int? PrijsInCenten,
        int AantalPersonen,
        Guid SupermarktId) : ICommand<Response>;
}
