using System.Text.Json.Serialization;

namespace Lutra.Infrastructure.OpenRouter;

internal sealed class AiVerspakketResponse
{
    [JsonPropertyName("naam")]
    public string? Naam { get; set; }

    [JsonPropertyName("supermarktNaam")]
    public string? SupermarktNaam { get; set; }

    [JsonPropertyName("prijsInCenten")]
    public int? PrijsInCenten { get; set; }

    [JsonPropertyName("aantalPersonen")]
    public int? AantalPersonen { get; set; }

    [JsonPropertyName("fotos")]
    public List<AiFoto>? Fotos { get; set; }

    [JsonPropertyName("ingredienten")]
    public List<AiIngredient>? Ingredienten { get; set; }

    [JsonPropertyName("voedingswaarden")]
    public List<AiVoedingswaarde>? Voedingswaarden { get; set; }

    [JsonPropertyName("allergenen")]
    public List<string>? Allergenen { get; set; }
}

internal sealed class AiFoto
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("isMainImage")]
    public bool IsMainImage { get; set; }
}

internal sealed class AiIngredient
{
    [JsonPropertyName("naam")]
    public string? Naam { get; set; }

    [JsonPropertyName("hoeveelheid")]
    public decimal? Hoeveelheid { get; set; }

    [JsonPropertyName("eenheid")]
    public string? Eenheid { get; set; }

    [JsonPropertyName("inbegrepen")]
    public bool Inbegrepen { get; set; }
}

internal sealed class AiVoedingswaarde
{
    [JsonPropertyName("basis")]
    public string? Basis { get; set; }

    [JsonPropertyName("energieKj")]
    public decimal? EnergieKj { get; set; }

    [JsonPropertyName("energieKcal")]
    public decimal? EnergieKcal { get; set; }

    [JsonPropertyName("vetten")]
    public decimal? Vetten { get; set; }

    [JsonPropertyName("waarvanVerzadigd")]
    public decimal? WaarvanVerzadigd { get; set; }

    [JsonPropertyName("koolhydraten")]
    public decimal? Koolhydraten { get; set; }

    [JsonPropertyName("waarvanSuikers")]
    public decimal? WaarvanSuikers { get; set; }

    [JsonPropertyName("vezels")]
    public decimal? Vezels { get; set; }

    [JsonPropertyName("eiwitten")]
    public decimal? Eiwitten { get; set; }

    [JsonPropertyName("zout")]
    public decimal? Zout { get; set; }
}
