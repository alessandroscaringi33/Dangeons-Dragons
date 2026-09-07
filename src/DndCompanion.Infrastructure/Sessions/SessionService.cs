using DndCompanion.Core.Domain.Entities;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.Sessions;
using DndCompanion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.Sessions;

/// <summary>
/// Manages the play sessions of a campaign using the campaign's SQLite
/// database. Every operation is saved immediately, providing autosave.
/// </summary>
public sealed class SessionService : ISessionService
{
    private readonly ICampaignDatabaseFactory _databaseFactory;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        ICampaignDatabaseFactory databaseFactory,
        ILogger<SessionService> logger)
    {
        _databaseFactory = databaseFactory;
        _logger = logger;
    }

    public async Task<SessionInfo?> GetActiveSessionAsync(
        string campaignFolderPath,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var campaign = await context.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (campaign is null || campaign.ActiveSessionId is not Guid activeId)
        {
            return null;
        }

        var session = await context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == activeId && s.IsActive, cancellationToken)
            .ConfigureAwait(false);

        return session is null ? null : await ToInfoAsync(context, session, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SessionInfo>> ListSessionsAsync(
        string campaignFolderPath,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var sessions = await context.Sessions
            .AsNoTracking()
            .OrderByDescending(s => s.Number)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new List<SessionInfo>(sessions.Count);
        foreach (var session in sessions)
        {
            result.Add(await ToInfoAsync(context, session, cancellationToken).ConfigureAwait(false));
        }

        return result;
    }

    public async Task<SessionInfo> StartSessionAsync(
        string campaignFolderPath,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var campaign = await context.Campaigns
            .Include(c => c.Sessions)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new SessionException("La campagna non è stata trovata o non è inizializzata.");

        var nextNumber = campaign.Sessions.Count == 0
            ? 1
            : campaign.Sessions.Max(s => s.Number) + 1;

        var session = new Session
        {
            CampaignId = campaign.Id,
            Number = nextNumber,
            Title = string.IsNullOrWhiteSpace(title) ? $"Sessione {nextNumber}" : title.Trim(),
            Summary = string.Empty,
            Notes = string.Empty
        };
        session.Start();

        context.Sessions.Add(session);

        campaign.ActiveSessionId = session.Id;
        campaign.Touch();

        context.SessionEvents.Add(new SessionEvent
        {
            SessionId = session.Id,
            Timestamp = session.StartedAt,
            Type = SessionEventType.SessionStarted,
            Description = $"Sessione {session.Number} iniziata."
        });

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Started session {Number} in campaign {Folder}", session.Number, campaignFolderPath);
        return await ToInfoAsync(context, session, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionInfo> OpenSessionAsync(
        string campaignFolderPath,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var campaign = await context.Campaigns
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new SessionException("La campagna non è stata trovata o non è inizializzata.");

        var session = await context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new SessionException("La sessione non è stata trovata.");

        // End any other active session.
        var otherActive = await context.Sessions
            .Where(s => s.CampaignId == campaign.Id && s.IsActive && s.Id != session.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var other in otherActive)
        {
            other.End();
        }

        session.Start();

        campaign.ActiveSessionId = session.Id;
        campaign.Touch();

        context.SessionEvents.Add(new SessionEvent
        {
            SessionId = session.Id,
            Timestamp = session.StartedAt,
            Type = SessionEventType.SessionStarted,
            Description = $"Sessione {session.Number} riaperta."
        });

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Opened session {Number} in campaign {Folder}", session.Number, campaignFolderPath);
        return await ToInfoAsync(context, session, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionInfo> CloseSessionAsync(
        string campaignFolderPath,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var campaign = await context.Campaigns
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new SessionException("La campagna non è stata trovata o non è inizializzata.");

        if (campaign.ActiveSessionId is not Guid activeId)
        {
            throw new SessionException("Non c'è una sessione attiva da chiudere.");
        }

        var session = await context.Sessions
            .FirstOrDefaultAsync(s => s.Id == activeId && s.IsActive, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new SessionException("La sessione attiva non è stata trovata.");

        session.End();

        campaign.ActiveSessionId = null;
        campaign.Touch();

        context.SessionEvents.Add(new SessionEvent
        {
            SessionId = session.Id,
            Timestamp = session.EndedAt!.Value,
            Type = SessionEventType.SessionEnded,
            Description = $"Sessione {session.Number} terminata."
        });

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Closed session {Number} in campaign {Folder}", session.Number, campaignFolderPath);
        return await ToInfoAsync(context, session, cancellationToken).ConfigureAwait(false);
    }

    public async Task<QuickNoteInfo> AddQuickNoteAsync(
        string campaignFolderPath,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new SessionException("La nota rapida non può essere vuota.");
        }

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var session = await RequireActiveSessionAsync(context, campaignFolderPath, cancellationToken).ConfigureAwait(false);

        var note = new Note
        {
            CampaignId = session.CampaignId,
            SessionId = session.Id,
            Title = "Nota rapida",
            Content = content.Trim(),
            IsPinned = true
        };

        context.Notes.Add(note);

        context.SessionEvents.Add(new SessionEvent
        {
            SessionId = session.Id,
            Timestamp = DateTime.UtcNow,
            Type = SessionEventType.NoteAdded,
            Description = $"Nota: {note.Content}"
        });

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Added quick note to session {Number} in {Folder}", session.Number, campaignFolderPath);
        return new QuickNoteInfo
        {
            Id = note.Id,
            SessionId = session.Id,
            Content = note.Content,
            Timestamp = note.CreatedAt
        };
    }

    public async Task<IReadOnlyList<QuickNoteInfo>> ListQuickNotesAsync(
        string campaignFolderPath,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var activeSession = await context.Campaigns
            .AsNoTracking()
            .Where(c => c.ActiveSessionId != null)
            .Select(c => c.ActiveSessionId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (activeSession is null)
        {
            return Array.Empty<QuickNoteInfo>();
        }

        var notes = await context.Notes
            .AsNoTracking()
            .Where(n => n.SessionId == activeSession && n.IsPinned)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return notes.Select(n => new QuickNoteInfo
        {
            Id = n.Id,
            SessionId = n.SessionId!.Value,
            Content = n.Content,
            Timestamp = n.CreatedAt
        }).ToList();
    }

    public async Task<SessionEventInfo> AddEventAsync(
        string campaignFolderPath,
        SessionEventType type,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new SessionException("La descrizione dell'evento non può essere vuota.");
        }

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var session = await RequireActiveSessionAsync(context, campaignFolderPath, cancellationToken).ConfigureAwait(false);

        var evt = new SessionEvent
        {
            SessionId = session.Id,
            Timestamp = DateTime.UtcNow,
            Type = type,
            Description = description.Trim()
        };

        context.SessionEvents.Add(evt);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Added event {Type} to session {Number} in {Folder}", type, session.Number, campaignFolderPath);
        return new SessionEventInfo
        {
            Id = evt.Id,
            SessionId = session.Id,
            Timestamp = evt.Timestamp,
            Type = evt.Type,
            Description = evt.Description
        };
    }

    public async Task<IReadOnlyList<SessionEventInfo>> ListRecentEventsAsync(
        string campaignFolderPath,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var activeSessionId = await context.Campaigns
            .AsNoTracking()
            .Where(c => c.ActiveSessionId != null)
            .Select(c => c.ActiveSessionId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (activeSessionId is null)
        {
            return Array.Empty<SessionEventInfo>();
        }

        var events = await context.SessionEvents
            .AsNoTracking()
            .Where(e => e.SessionId == activeSessionId)
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return events.Select(e => new SessionEventInfo
        {
            Id = e.Id,
            SessionId = e.SessionId,
            Timestamp = e.Timestamp,
            Type = e.Type,
            Description = e.Description
        }).ToList();
    }

    private static async Task<Session> RequireActiveSessionAsync(
        CampaignDbContext context,
        string campaignFolderPath,
        CancellationToken cancellationToken)
    {
        var campaign = await context.Campaigns
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new SessionException("La campagna non è stata trovata o non è inizializzata.");

        if (campaign.ActiveSessionId is not Guid activeId)
        {
            throw new SessionException("Non c'è una sessione attiva.");
        }

        return await context.Sessions
            .FirstOrDefaultAsync(s => s.Id == activeId && s.IsActive, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new SessionException("La sessione attiva non è stata trovata.");
    }

    private static async Task<SessionInfo> ToInfoAsync(
        CampaignDbContext context,
        Session session,
        CancellationToken cancellationToken)
    {
        var eventCount = await context.SessionEvents
            .CountAsync(e => e.SessionId == session.Id, cancellationToken)
            .ConfigureAwait(false);

        var noteCount = await context.Notes
            .CountAsync(n => n.SessionId == session.Id && n.IsPinned, cancellationToken)
            .ConfigureAwait(false);

        return new SessionInfo
        {
            Id = session.Id,
            CampaignId = session.CampaignId,
            Number = session.Number,
            Title = session.Title,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            IsActive = session.IsActive,
            Summary = session.Summary,
            Notes = session.Notes,
            EventCount = eventCount,
            QuickNoteCount = noteCount
        };
    }

    private static async Task SaveAsync(CampaignDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new SessionException("Non è stato possibile salvare la sessione.", ex);
        }
    }
}