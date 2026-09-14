using System.Text.RegularExpressions;
using Lutra.Application.Models.Verspakketten;

namespace Lutra.Application.Verspakketten;

/// <summary>
/// Cleans AI-extracted values so they always fit the persistence constraints
/// (column lengths and decimal precision) instead of failing the import.
/// </summary>
public static partial class VerspakketImportSanitizer
{
    public const int MaxNaamLength = 50;
    public const int MaxIngredientNaamLength = 100;

    /// <summary>Largest value that fits a numeric(10,2) column.</summary>
    public const decimal MaxDecimalValue = 99_999_999.99m;

    private static readonly char[] TrailingSeparators = [' ', '-', '|', ',', ';', ':', '.', '/'];

    public static string? CleanNaam(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var collapsed = WhitespaceRegex().Replace(value, " ").Trim();

        if (collapsed.Length > maxLength)
        {
            collapsed = TruncateAtWord(collapsed, maxLength);
        }

        collapsed = collapsed.TrimEnd(TrailingSeparators);
        return string.IsNullOrWhiteSpace(collapsed) ? null : collapsed;
    }

    public static IReadOnlyList<Ingredient> CleanIngredienten(IReadOnlyList<Ingredient> ingredienten)
    {
        var result = new List<Ingredient>(ingredienten.Count);

        foreach (var ingredient in ingredienten)
        {
            var naam = CleanNaam(ingredient.Naam, MaxIngredientNaamLength);

            if (naam is null)
            {
                continue;
            }

            var hoeveelheid = Math.Round(ingredient.Hoeveelheid, 2, MidpointRounding.AwayFromZero);

            if (hoeveelheid <= 0 || hoeveelheid > MaxDecimalValue)
            {
                continue;
            }

            result.Add(ingredient with { Naam = naam, Hoeveelheid = hoeveelheid });
        }

        return result;
    }

    public static IReadOnlyList<Voedingswaarde> CleanVoedingswaarden(IReadOnlyList<Voedingswaarde> voedingswaarden)
    {
        var result = new List<Voedingswaarde>(voedingswaarden.Count);

        foreach (var voedingswaarde in voedingswaarden)
        {
            var vetten = Round(voedingswaarde.Vetten);
            var koolhydraten = Round(voedingswaarde.Koolhydraten);

            result.Add(new Voedingswaarde
            {
                Basis = voedingswaarde.Basis,
                EnergieKj = Round(voedingswaarde.EnergieKj),
                EnergieKcal = Round(voedingswaarde.EnergieKcal),
                Vetten = vetten,
                WaarvanVerzadigd = ClampTo(vetten, Round(voedingswaarde.WaarvanVerzadigd)),
                Koolhydraten = koolhydraten,
                WaarvanSuikers = ClampTo(koolhydraten, Round(voedingswaarde.WaarvanSuikers)),
                Vezels = Round(voedingswaarde.Vezels),
                Eiwitten = Round(voedingswaarde.Eiwitten),
                Zout = Round(voedingswaarde.Zout)
            });
        }

        return result;
    }

    private static decimal? Round(decimal? value) =>
        value is null ? null : Math.Round(Math.Clamp(value.Value, 0, MaxDecimalValue), 2, MidpointRounding.AwayFromZero);

    private static decimal? ClampTo(decimal? total, decimal? part) =>
        total is not null && part is not null && part > total ? total : part;

    private static string TruncateAtWord(string value, int maxLength)
    {
        var cut = value[..maxLength];
        var lastSpace = cut.LastIndexOf(' ');

        if (lastSpace >= maxLength / 2)
        {
            cut = cut[..lastSpace];
        }

        return cut;
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}