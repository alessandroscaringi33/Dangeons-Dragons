namespace DndCompanion.Core.Quests;

/// <summary>
/// Exception thrown by the quest service when a quest cannot be created,
/// updated, deleted, searched or when its status or links cannot be changed.
/// </summary>
public sealed class QuestException : Exception
{
    public QuestException(string message)
        : base(message)
    {
    }

    public QuestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}