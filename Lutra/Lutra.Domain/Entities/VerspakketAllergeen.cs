namespace Lutra.Domain.Entities;

public class VerspakketAllergeen : BaseEntity
{
    public required Allergeen Allergeen { get; set; }

    public required Guid VerspakketId { get; set; }

    public virtual Verspakket Verspakket { get; set; } = null!;
}
