using System.ComponentModel.DataAnnotations;

namespace Lutra.API.Requests;

/// <summary>
/// Represents the data required to update a verspakket.
/// </summary>
public sealed record UpdateVerspakketRequest(
    [Required, MaxLength(50)] string Naam,
    [Range(0, int.MaxValue)] int PrijsInCenten,
    [Range(1, 10)] int AantalPersonen,
    [Required] Guid SupermarktId,
    IReadOnlyList<VerspakketFotoRequest>? Fotos = null);


