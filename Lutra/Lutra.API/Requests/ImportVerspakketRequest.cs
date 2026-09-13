using System.ComponentModel.DataAnnotations;

namespace Lutra.API.Requests;

/// <summary>
/// Represents a request to import a verspakket from a retailer product page.
/// </summary>
public sealed record ImportVerspakketRequest(
    [Required, Url] string Url);
