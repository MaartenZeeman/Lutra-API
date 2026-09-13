namespace Lutra.Application.Models.Verspakketten;

/// <summary>
/// Product details that were extracted from a retailer product page by the AI extractor.
/// Values that could not be determined are left null or empty.
/// </summary>
public sealed record ExtractedVerspakket
{
    public required string Naam { get; init; }

    public string? SupermarktNaam { get; init; }

    public int? PrijsInCenten { get; init; }

    public int? AantalPersonen { get; init; }

    public IReadOnlyList<VerspakketFoto> Fotos { get; init; } = [];

    public IReadOnlyList<Ingredient> Ingredienten { get; init; } = [];

    public IReadOnlyList<Voedingswaarde> Voedingswaarden { get; init; } = [];

    public IReadOnlyList<Domain.Entities.Allergeen> Allergenen { get; init; } = [];
}
