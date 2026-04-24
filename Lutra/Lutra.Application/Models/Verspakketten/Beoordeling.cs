using System.ComponentModel.DataAnnotations;

namespace Lutra.Application.Models.Verspakketten;

public class Beoordeling
{
    [Range(1, 10)]
    public required int CijferSmaak { get; init; }

    [Range(1, 10)]
    public required int CijferBereiden { get; init; }

    public required bool Aanbevolen { get; init; }

    [MaxLength(1024)]
    public string? Tekst { get; init; }
}
