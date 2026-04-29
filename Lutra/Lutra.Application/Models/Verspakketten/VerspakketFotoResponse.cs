namespace Lutra.Application.Models.Verspakketten;

public sealed record VerspakketFotoResponse(
    Guid Id,
    /// <summary>Base64-encoded image data.</summary>
    string Base64Data,
    bool IsMainImage);
