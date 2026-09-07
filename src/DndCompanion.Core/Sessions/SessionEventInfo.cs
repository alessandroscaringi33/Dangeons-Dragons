using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Sessions;

/// <summary>UI-facing entry of the session timeline.</summary>
public sealed class SessionEventInfo
{
    public Guid Id { get; init; }

    public Guid SessionId { get; init; }

    public DateTime Timestamp { get; init; }

    public SessionEventType Type { get; init; }

    public string Description { get; init; } = string.Empty;
}