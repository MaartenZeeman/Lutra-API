using System.ComponentModel.DataAnnotations;

namespace Lutra.Domain.Entities;

public class Beoordeling : BaseEntity
{
    [Range(1, 10)]
    public required int CijferSmaak { get; set; }

    [Range(1, 10)]
    public required int CijferBereiden { get; set; }

    public required bool Aanbevolen { get; set; }

    [MaxLength(1024)]
    public string? Tekst { get; set; }
}
