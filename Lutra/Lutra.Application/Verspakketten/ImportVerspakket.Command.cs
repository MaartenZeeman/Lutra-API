using Cortex.Mediator.Commands;

namespace Lutra.Application.Verspakketten;

public sealed partial class ImportVerspakket
{
    /// <summary>Imports a verspakket from a retailer product page URL.</summary>
    public sealed record Command(string Url) : ICommand<Response>;
}
