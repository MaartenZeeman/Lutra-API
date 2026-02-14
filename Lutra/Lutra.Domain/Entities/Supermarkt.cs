using System.ComponentModel.DataAnnotations;

namespace Lutra.Domain.Entities;

public class Supermarkt : BaseEntity
{
    [MaxLength(50)]
    public required string Naam { get; set; }
}
