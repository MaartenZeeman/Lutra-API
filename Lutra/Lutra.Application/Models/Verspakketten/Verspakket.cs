using Lutra.Application.Models.Supermarkten;

namespace Lutra.Application.Models.Verspakketten
{
    public record Verspakket
    {
        public required string Name { get; init; }
        public required string Rating { get; init; }
        public Supermarkt Supermarkt { get; init; }
    }
}
