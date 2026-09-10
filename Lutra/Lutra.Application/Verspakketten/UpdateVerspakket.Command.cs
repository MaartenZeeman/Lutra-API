using Cortex.Mediator.Commands;
using Lutra.Application.Models.Verspakketten;

namespace Lutra.Application.Verspakketten;

public sealed partial class UpdateVerspakket
{
    /// <summary>
    /// Updates an existing verspakket.
    /// </summary>
    public sealed record Command(
        Guid Id,
        string Naam,
        int PrijsInCenten,
        int AantalPersonen,
        Guid SupermarktId,
        IReadOnlyList<VerspakketFoto>? Fotos = null,
        IReadOnlyList<Ingredient>? Ingredienten = null,
        IReadOnlyList<Voedingswaarde>? Voedingswaarden = null,
        IReadOnlyList<Domain.Entities.Allergeen>? Allergenen = null) : ICommand<Response>;
}
