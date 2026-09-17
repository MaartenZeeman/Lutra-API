using FluentAssertions;
using Lutra.Application.Models.Verspakketten;
using Lutra.Application.Verspakketten;

namespace Lutra.Application.UnitTests.Verspakketten;

public class VerspakketImportSanitizerTests
{
    [Fact]
    public void CleanNaam_LongName_TruncatesToMaxLengthAtWordBoundary()
    {
        var value = "Jumbo Satépannetje Gesneden Verspakket 4 Personen met extra veel groenten en verse kruidenboter, romige saus, krokante aardappelpartjes en een lekker bijgerecht voor het hele gezin, samengesteld door onze chef met zorg voor smaak en kwaliteit elke dag opnieuw geselecteerd voor de beste kwaliteit en verse smaakbeleving";

        var result = VerspakketImportSanitizer.CleanNaam(value, VerspakketImportSanitizer.MaxNaamLength);

        result.Should().NotBeNull();
        result!.Length.Should().BeLessThanOrEqualTo(VerspakketImportSanitizer.MaxNaamLength);
        result.Should().Be("Jumbo Satépannetje Gesneden Verspakket 4 Personen met extra veel groenten en verse kruidenboter, romige saus, krokante aardappelpartjes en een lekker bijgerecht voor het hele gezin, samengesteld door onze chef met zorg voor smaak en kwaliteit elke dag");
    }

    [Fact]
    public void CleanNaam_CollapsesWhitespaceAndTrimsTrailingSeparators()
    {
        var result = VerspakketImportSanitizer.CleanNaam("  Pasta   Bolognese |  ", VerspakketImportSanitizer.MaxNaamLength);

        result.Should().Be("Pasta Bolognese");
    }

    [Fact]
    public void CleanNaam_EmptyValue_ReturnsNull()
    {
        VerspakketImportSanitizer.CleanNaam("   ", VerspakketImportSanitizer.MaxNaamLength).Should().BeNull();
    }

    [Fact]
    public void CleanIngredienten_TruncatesNameAndRoundsHoeveelheid()
    {
        var longName = new string('a', 150);

        var result = VerspakketImportSanitizer.CleanIngredienten(
        [
            new Ingredient(longName, 400.126m, Domain.Entities.Eenheid.Gram, true),
            new Ingredient("Te weinig", 0m, Domain.Entities.Eenheid.Gram, true)
        ]);

        result.Should().ContainSingle();
        result[0].Naam.Length.Should().Be(VerspakketImportSanitizer.MaxIngredientNaamLength);
        result[0].Hoeveelheid.Should().Be(400.13m);
    }

    [Fact]
    public void CleanVoedingswaarden_RoundsAndClampsSubsetValues()
    {
        var result = VerspakketImportSanitizer.CleanVoedingswaarden(
        [
            new Voedingswaarde
            {
                Basis = Domain.Entities.VoedingswaardeBasis.Per100Gram,
                Vetten = 2.005m,
                WaarvanVerzadigd = 3.129m,
                Koolhydraten = 10.556m,
                WaarvanSuikers = 12.994m
            }
        ]);

        result.Should().ContainSingle();
        result[0].Vetten.Should().Be(2.01m);
        result[0].WaarvanVerzadigd.Should().Be(2.01m);
        result[0].Koolhydraten.Should().Be(10.56m);
        result[0].WaarvanSuikers.Should().Be(10.56m);
    }
}