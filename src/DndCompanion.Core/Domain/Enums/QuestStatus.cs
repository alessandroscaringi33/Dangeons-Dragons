namespace DndCompanion.Core.Domain.Enums;

/// <summary>Current state of a <see cref="Entities.Quest"/>.</summary>
public enum QuestStatus
{
    NotStarted,
    Active,
    Completed,
    Failed,
    Abandoned
}
