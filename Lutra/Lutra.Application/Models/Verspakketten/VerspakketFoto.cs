namespace Lutra.Application.Models.Verspakketten;

/// <summary>
/// Represents a foto to associate with a verspakket.
/// </summary>
public sealed record VerspakketFoto(
    /// <summary>Base64-encoded image data.</summary>
    string Base64Data,
    bool IsMainImage);
