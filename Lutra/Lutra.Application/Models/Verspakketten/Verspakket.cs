using Lutra.Application.Models.Supermarkten;

namespace Lutra.Application.Models.Verspakketten;

public record Verspakket
{
    public required Guid Id { get; init; }

    public required string Naam { get; init; }

    public int? PrijsInCenten { get; init; }

    public int AantalPersonen { get; init; }

    public double? AverageCijferSmaak { get; init; }

    public double? AverageCijferBereiden { get; init; }

    public Beoordeling[]? Beoordelingen { get; init; }

    public VerspakketFotoResponse[]? Fotos { get; init; }

    public Ingredient[]? Ingredienten { get; init; }

    public Voedingswaarde[]? Voedingswaarden { get; init; }

    public Domain.Entities.Allergeen[]? Allergenen { get; init; }

    public Supermarkt? Supermarkt { get; init; }
}
