using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Sessions;

/// <summary>
/// Application service that manages the play sessions of a campaign. A session
/// is the operational centre during the game: it tracks its own timeline,
/// quick notes and status. Every operation persists immediately (autosave).
/// </summary>
public interface ISessionService
{
    /// <summary>Returns the currently active session of the campaign, if any.</summary>
    Task<SessionInfo?> GetActiveSessionAsync(
        string campaignFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>Lists the sessions of the campaign, most recent first.</summary>
    Task<IReadOnlyList<SessionInfo>> ListSessionsAsync(
        string campaignFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>Starts a new session: increments the session number, activates
    /// it and sets it as the campaign's active session.</summary>
    Task<SessionInfo> StartSessionAsync(
        string campaignFolderPath,
        string? title = null,
        CancellationToken cancellationToken = default);

    /// <summary>Reopens an ended session and sets it as active again.</summary>
    /// <exception cref="SessionException">When the session does not exist.</exception>
    Task<SessionInfo> OpenSessionAsync(
        string campaignFolderPath,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>Closes the active session, recording its end timestamp.</summary>
    /// <exception cref="SessionException">When there is no active session.</exception>
    Task<SessionInfo> CloseSessionAsync(
        string campaignFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a quick note to the active session.</summary>
    /// <exception cref="SessionException">When there is no active session.</exception>
    Task<QuickNoteInfo> AddQuickNoteAsync(
        string campaignFolderPath,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the recent quick notes of the active session.</summary>
    Task<IReadOnlyList<QuickNoteInfo>> ListQuickNotesAsync(
        string campaignFolderPath,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Adds an event to the active session timeline.</summary>
    /// <exception cref="SessionException">When there is no active session.</exception>
    Task<SessionEventInfo> AddEventAsync(
        string campaignFolderPath,
        SessionEventType type,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the recent timeline events of the active session.</summary>
    Task<IReadOnlyList<SessionEventInfo>> ListRecentEventsAsync(
        string campaignFolderPath,
        int limit = 20,
        CancellationToken cancellationToken = default);
}