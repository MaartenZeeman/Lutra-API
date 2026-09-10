using System.ComponentModel.DataAnnotations;

namespace Lutra.Domain.Entities;

public class Voedingswaarde : BaseEntity
{
    public required VoedingswaardeBasis Basis { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? EnergieKj { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? EnergieKcal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Vetten { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? WaarvanVerzadigd { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Koolhydraten { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? WaarvanSuikers { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Vezels { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Eiwitten { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Zout { get; set; }

    public required Guid VerspakketId { get; set; }

    public virtual Verspakket Verspakket { get; set; } = null!;
}
