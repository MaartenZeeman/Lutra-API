using System.ComponentModel.DataAnnotations;
using Lutra.Domain.Entities;

namespace Lutra.API.Requests;

/// <summary>
/// Represents the nutritional values of a verspakket. Weight values are in grams,
/// energy in kJ and kcal. The basis indicates whether the values are per 100 gram or per portie.
/// </summary>
public sealed record VoedingswaardeRequest(
    [EnumDataType(typeof(VoedingswaardeBasis))] VoedingswaardeBasis Basis,
    [Range(0, 999999999999.99)] decimal? EnergieKj,
    [Range(0, 999999999999.99)] decimal? EnergieKcal,
    [Range(0, 999999999999.99)] decimal? Vetten,
    [Range(0, 999999999999.99)] decimal? WaarvanVerzadigd,
    [Range(0, 999999999999.99)] decimal? Koolhydraten,
    [Range(0, 999999999999.99)] decimal? WaarvanSuikers,
    [Range(0, 999999999999.99)] decimal? Vezels,
    [Range(0, 999999999999.99)] decimal? Eiwitten,
    [Range(0, 999999999999.99)] decimal? Zout);
