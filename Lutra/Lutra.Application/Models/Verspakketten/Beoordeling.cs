namespace Lutra.Application.Models.Verspakketten;

public record Beoordeling
{
    public required int CijferSmaak { get; init; }

    public required int CijferBereiden { get; init; }

    public required bool Aanbevolen { get; init; }

    public string? Tekst { get; init; }
}
