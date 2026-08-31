namespace Lutra.Application.Models.Verspakketten;

/// <summary>
/// Represents the nutritional values of a verspakket following the standard
/// Dutch/European nutrition label. Weight values are in grams, energy in kJ and kcal.
/// </summary>
public sealed record Voedingswaarde
{
    public decimal? EnergieKj { get; init; }

    public decimal? EnergieKcal { get; init; }

    public decimal? Vetten { get; init; }

    public decimal? WaarvanVerzadigd { get; init; }

    public decimal? Koolhydraten { get; init; }

    public decimal? WaarvanSuikers { get; init; }

    public decimal? Vezels { get; init; }

    public decimal? Eiwitten { get; init; }

    public decimal? Zout { get; init; }
}
