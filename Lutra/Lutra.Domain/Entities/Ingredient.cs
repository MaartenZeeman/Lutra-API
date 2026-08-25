using System.ComponentModel.DataAnnotations;

namespace Lutra.Domain.Entities;

public class Ingredient : BaseEntity
{
    [MaxLength(100)]
    public required string Naam { get; set; }

    [Range(0.01, double.MaxValue)]
    public required decimal Hoeveelheid { get; set; }

    public required Eenheid Eenheid { get; set; }

    public required bool Inbegrepen { get; set; }

    public required Guid VerspakketId { get; set; }

    public virtual Verspakket Verspakket { get; set; } = null!;
}