using System.ComponentModel.DataAnnotations;

namespace Lutra.API.Requests;

/// <summary>
/// Represents the data required to create a verspakket.
/// </summary>
public sealed record CreateVerspakketRequest(
    [Required, MaxLength(50)] string Naam,
    [Range(0, int.MaxValue)] int? PrijsInCenten,
    [Range(1, 10)] int AantalPersonen,
    [Required] Guid SupermarktId,
    AddBeoordelingRequest? Beoordeling = null,
    IReadOnlyList<VerspakketFotoRequest>? Fotos = null);