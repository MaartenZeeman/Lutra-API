using Lutra.Application.Models.Verspakketten;

namespace Lutra.Application.Verspakketten;

public sealed partial class GetVerspakketten
{
    public sealed record Response
    {
        public required IEnumerable<VerspakketSummary> Verspakketten { get; init; }
    }
}
