using Cortex.Mediator.Commands;
using Lutra.Application.Models.Verspakketten;

namespace Lutra.Application.Verspakketten;

public sealed partial class CreateVerspakket
{
    public sealed record Command(
        string Naam,
        int? PrijsInCenten,
        int AantalPersonen,
        Guid SupermarktId,
        Beoordeling? Beoordeling,
        IReadOnlyList<VerspakketFoto>? Fotos = null,
        IReadOnlyList<Ingredient>? Ingredienten = null,
        Voedingswaarde? Voedingswaarde = null,
        IReadOnlyList<Domain.Entities.Allergeen>? Allergenen = null) : ICommand<Response>;
}
