using System.ComponentModel.DataAnnotations;
using Lutra.Domain.Entities;

namespace Lutra.API.Requests;

/// <summary>
/// Represents an ingredient in a request.
/// </summary>
public sealed record IngredientRequest(
    [Required, MaxLength(100)] string Naam,
    [Range(0.01, 999999999999.99)] decimal Hoeveelheid,
    [EnumDataType(typeof(Eenheid))] Eenheid Eenheid,
    bool Inbegrepen);