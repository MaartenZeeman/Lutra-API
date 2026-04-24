using Cortex.Mediator.Commands;
using Lutra.Application.Models.Verspakketten;
using System.ComponentModel.DataAnnotations;

namespace Lutra.Application.Verspakketten;

public sealed partial class CreateVerspakket
{
    public sealed record Command(
        string Naam,
        int? PrijsInCenten,
        [Range(1, 10)] int AantalPersonen,
        Guid SupermarktId,
        Beoordeling? Beoordeling) : ICommand<Response>;
}
