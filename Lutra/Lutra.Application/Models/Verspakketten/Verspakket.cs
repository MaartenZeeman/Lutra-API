using Lutra.Application.Models.Supermarkten;

namespace Lutra.Application.Models.Verspakketten
{
    public record Verspakket
    {
        public required string Naam { get; init; }
        public int? PrijsInCenten { get; init; }
        public Beoordeling[]? Beoordelingen { get; init; }
        public Supermarkt? Supermarkt { get; init; }
    }
}
