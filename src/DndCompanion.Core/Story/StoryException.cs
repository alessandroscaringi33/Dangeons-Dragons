namespace DndCompanion.Core.Story;

/// <summary>
/// Exception thrown by the story services when a chapter or scene cannot be
/// created, updated, deleted or reordered.
/// </summary>
public sealed class StoryException : Exception
{
    public StoryException(string message)
        : base(message)
    {
    }

    public StoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}