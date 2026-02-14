using System.ComponentModel.DataAnnotations;

namespace Lutra.Domain.Entities;

public class Verspakket : BaseEntity
{
    [MaxLength(50)]
    public required string Naam { get; set; }

    [Range(1, 100)]
    public int Cijfer { get; set; }

    [MaxLength(255)]
    public string? Opmerking { get; set; }

    [Range(1, 10)]
    public int AantalPersonen {  get; set; }

    public required Guid SupermarktId { get; set; }

    public required virtual Supermarkt Supermarkt { get; set; }
}

