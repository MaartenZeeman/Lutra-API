using System.ComponentModel.DataAnnotations;

namespace Lutra.API.Requests;

/// <summary>
/// Represents the data required to create or update a supermarkt.
/// </summary>
public sealed record SupermarktRequest(
    [Required, MaxLength(50)] string Naam);