using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Verspakketten;

namespace Lutra.API.IntegrationTests.Infrastructure;

/// <summary>
/// Deterministic stand-in for the OpenRouter extractor so integration tests never call the network.
/// URLs containing "transient-fail" or "permanent-fail" simulate the corresponding failures.
/// </summary>
public sealed class FakeVerspakketProductExtractor : IVerspakketProductExtractor
{
    private const string ValidBase64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI6QAAAABJRU5ErkJggg==";

    public Task<ExtractedVerspakket> ExtractAsync(string url, CancellationToken cancellationToken)
    {
        if (url.Contains("transient-fail", StringComparison.OrdinalIgnoreCase))
        {
            throw new ExternalServiceException("Tijdelijke fout bij het ophalen van de productpagina.");
        }

        if (url.Contains("permanent-fail", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnprocessableException("De productpagina bevatte onvoldoende gegevens.");
        }

        return Task.FromResult(new ExtractedVerspakket
        {
            Naam = "AI Test Verspakket",
            SupermarktNaam = "Albert Heijn",
            PrijsInCenten = 349,
            AantalPersonen = 2,
            Fotos = [new VerspakketFoto(ValidBase64Png, IsMainImage: true)],
            Ingredienten = [new Ingredient("Tomaten", 400, Lutra.Domain.Entities.Eenheid.Gram, true)],
            Allergenen = [Lutra.Domain.Entities.Allergeen.Gluten]
        });
    }
}
