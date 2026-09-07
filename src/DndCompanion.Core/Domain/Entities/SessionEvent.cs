using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// An entry of the session timeline, describing what happened at a given
/// moment and optionally linking to the related domain objects.
/// </summary>
public class SessionEvent : Entity
{
    public Guid SessionId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public SessionEventType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public Guid? RelatedCharacterId { get; set; }

    public Guid? RelatedNpcId { get; set; }

    public Guid? RelatedSceneId { get; set; }

    public Guid? RelatedDiceRollId { get; set; }

    public Guid? RelatedCombatId { get; set; }
}
