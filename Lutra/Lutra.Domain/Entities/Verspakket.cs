using System.ComponentModel.DataAnnotations;

namespace Lutra.Domain.Entities;

public class Verspakket : BaseEntity
{
    private readonly List<Beoordeling> _beoordelingen = [];

    [MaxLength(50)]
    public required string Naam { get; set; }

    public int? PrijsInCenten { get; set; }

    [Range(1, 10)]
    public int AantalPersonen { get; set; }

    public required Guid SupermarktId { get; set; }

    public virtual Supermarkt Supermarkt { get; set; } = null!;

    public IReadOnlyCollection<Beoordeling> Beoordelingen => _beoordelingen.AsReadOnly();

    public void AddBeoordeling(Beoordeling beoordeling)
    {
        _beoordelingen.Add(beoordeling);
    }

    public bool RemoveBeoordeling(Guid id)
    {
        var beoordeling = _beoordelingen.Find(b => b.Id == id);
        if (beoordeling is null)
            return false;

        _beoordelingen.Remove(beoordeling);
        return true;
    }
}