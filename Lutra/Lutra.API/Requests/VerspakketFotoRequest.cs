using System.ComponentModel.DataAnnotations;

namespace Lutra.API.Requests;

/// <summary>
/// Represents a foto in a request, encoded as base64.
/// </summary>
public sealed record VerspakketFotoRequest(
    [Required] string Base64Data,
    bool IsMainImage);