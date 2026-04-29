using Lutra.Application.Models.Supermarkten;

namespace Lutra.Application.Models.Verspakketten;

public record VerspakketSummary
{
    public required Guid Id { get; init; }

    public required string Naam { get; init; }

    public int? PrijsInCenten { get; init; }

    public int AantalPersonen { get; init; }

    public double? AverageCijferSmaak { get; init; }

    public double? AverageCijferBereiden { get; init; }

    public VerspakketFotoResponse? Foto { get; init; }

    public Supermarkt? Supermarkt { get; init; }
}