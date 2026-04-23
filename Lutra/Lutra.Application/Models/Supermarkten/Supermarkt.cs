namespace Lutra.Application.Models.Supermarkten
{
    public record Supermarkt
    {
        public required Guid Id { get; init; }

        public required string Naam { get; init; }
    }
}
