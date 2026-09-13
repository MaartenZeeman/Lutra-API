using System.ComponentModel.DataAnnotations;

namespace Lutra.Domain.Entities;

public class Verspakket : BaseEntity
{
    private readonly List<Beoordeling> _beoordelingen = [];
    private readonly List<VerspakketFoto> _fotos = [];
    private readonly List<Ingredient> _ingredienten = [];
    private readonly List<Voedingswaarde> _voedingswaarden = [];
    private readonly List<VerspakketAllergeen> _allergenen = [];

    [MaxLength(50)]
    public required string Naam { get; set; }

    [Range(0, int.MaxValue)]
    public int? PrijsInCenten { get; set; }

    [Range(1, 10)]
    public required int AantalPersonen { get; set; }

    [MaxLength(2048)]
    public string? BronUrl { get; set; }

    public required Guid SupermarktId { get; set; }

    public virtual Supermarkt Supermarkt { get; set; } = null!;

    public IReadOnlyCollection<Beoordeling> Beoordelingen => _beoordelingen.AsReadOnly();

    public IReadOnlyCollection<VerspakketFoto> Fotos => _fotos.AsReadOnly();

    public IReadOnlyCollection<Ingredient> Ingredienten => _ingredienten.AsReadOnly();

    public IReadOnlyCollection<Voedingswaarde> Voedingswaarden => _voedingswaarden.AsReadOnly();

    public IReadOnlyCollection<VerspakketAllergeen> Allergenen => _allergenen.AsReadOnly();

    public void AddBeoordeling(Beoordeling beoordeling)
    {
        _beoordelingen.Add(beoordeling);
    }

    public void AddFoto(VerspakketFoto foto)
    {
        _fotos.Add(foto);
    }

    public void AddIngredient(Ingredient ingredient)
    {
        _ingredienten.Add(ingredient);
    }

    public void AddVoedingswaarde(Voedingswaarde voedingswaarde)
    {
        _voedingswaarden.Add(voedingswaarde);
    }

    public void AddAllergeen(VerspakketAllergeen allergeen)
    {
        _allergenen.Add(allergeen);
    }

    public bool RemoveBeoordeling(Guid id)
    {
        var beoordeling = _beoordelingen.Find(b => b.Id == id);
        if (beoordeling is null)
            return false;

        _beoordelingen.Remove(beoordeling);
        return true;
    }

    public bool RemoveFoto(Guid id)
    {
        var foto = _fotos.Find(f => f.Id == id);
        if (foto is null)
            return false;

        _fotos.Remove(foto);
        return true;
    }

    public bool RemoveIngredient(Guid id)
    {
        var ingredient = _ingredienten.Find(i => i.Id == id);
        if (ingredient is null)
            return false;

        _ingredienten.Remove(ingredient);
        return true;
    }
}