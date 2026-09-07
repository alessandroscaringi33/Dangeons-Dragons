namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// Base class for all domain entities. Provides a globally unique identity.
/// </summary>
public abstract class Entity
{
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; init; }
}
