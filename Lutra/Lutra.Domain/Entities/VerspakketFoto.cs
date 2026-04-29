namespace Lutra.Domain.Entities;

public class VerspakketFoto : BaseEntity
{
    public required byte[] Data { get; set; }

    public required bool IsMainImage { get; set; }

    public required Guid VerspakketId { get; set; }

    public virtual Verspakket Verspakket { get; set; } = null!;
}
