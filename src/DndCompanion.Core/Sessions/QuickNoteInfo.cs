namespace DndCompanion.Core.Sessions;

/// <summary>UI-facing quick note attached to a session.</summary>
public sealed class QuickNoteInfo
{
    public Guid Id { get; init; }

    public Guid SessionId { get; init; }

    public string Content { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; }
}