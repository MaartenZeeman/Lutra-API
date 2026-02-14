namespace Lutra.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAt {  get; set; }

    public DateTime ModifiedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}
