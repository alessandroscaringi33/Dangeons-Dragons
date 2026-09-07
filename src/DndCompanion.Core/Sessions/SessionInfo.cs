namespace DndCompanion.Core.Sessions;

/// <summary>
/// UI-facing description of a play session.
/// </summary>
public sealed class SessionInfo
{
    public Guid Id { get; init; }

    public Guid CampaignId { get; init; }

    /// <summary>Progressive session number within the campaign.</summary>
    public int Number { get; init; }

    public string Title { get; init; } = string.Empty;

    public DateTime StartedAt { get; init; }

    public DateTime? EndedAt { get; init; }

    public bool IsActive { get; init; }

    public string Summary { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public int EventCount { get; init; }

    public int QuickNoteCount { get; init; }
}