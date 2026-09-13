using Lutra.Application.Models.Verspakketten;

namespace Lutra.Application.Interfaces;

/// <summary>
/// Fetches a retailer product page and extracts structured verspakket details,
/// including downloaded product photos, using an AI model.
/// </summary>
public interface IVerspakketProductExtractor
{
    Task<ExtractedVerspakket> ExtractAsync(string url, CancellationToken cancellationToken);
}
