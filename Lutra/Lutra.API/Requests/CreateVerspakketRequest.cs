using System.ComponentModel.DataAnnotations;
using Lutra.Domain.Entities;

namespace Lutra.API.Requests;

/// <summary>
/// Represents the data required to create a verspakket.
/// </summary>
public sealed record CreateVerspakketRequest(
    [Required, MaxLength(255)] string Naam,
    [Range(0, int.MaxValue)] int? PrijsInCenten,
    [Range(1, 10)] int AantalPersonen,
    [Required] Guid SupermarktId,
    AddBeoordelingRequest? Beoordeling = null,
    IReadOnlyList<VerspakketFotoRequest>? Fotos = null,
    IReadOnlyList<IngredientRequest>? Ingredienten = null,
    IReadOnlyList<VoedingswaardeRequest>? Voedingswaarden = null,
    IReadOnlyList<Domain.Entities.Allergeen>? Allergenen = null);