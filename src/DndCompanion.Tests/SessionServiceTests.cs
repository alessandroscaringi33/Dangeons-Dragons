using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.Sessions;
using DndCompanion.Infrastructure.Data;
using DndCompanion.Infrastructure.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DndCompanion.Tests;

/// <summary>
/// Tests for <see cref="SessionService"/>. Every test uses a fresh temporary
/// campaign folder and database, mirroring the real <c>Campagne</c> layout.
/// </summary>
public sealed class SessionServiceTests
{
    private readonly CampaignDatabaseFactory _factory = new();
    private readonly CampaignDatabaseInitializer _initializer;

    public SessionServiceTests()
    {
        _initializer = new CampaignDatabaseInitializer(
            _factory,
            NullLogger<CampaignDatabaseInitializer>.Instance);
    }

    private async Task<string> CreateCampaignFolderAsync()
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            "DndCompanionSessionTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(folder);
        await _initializer.InitializeAsync(folder);

        await using var context = _factory.CreateContext(folder);
        context.Campaigns.Add(new Core.Domain.Entities.Campaign { Name = Path.GetFileName(folder) });
        await context.SaveChangesAsync();

        return folder;
    }

    private SessionService CreateService()
    {
        return new SessionService(_factory, NullLogger<SessionService>.Instance);
    }

    // ---------- Start ----------

    [Fact]
    public async Task StartSession_CreatesNumberOne_AndActivates()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var session = await service.StartSessionAsync(folder);

        Assert.Equal(1, session.Number);
        Assert.True(session.IsActive);
        Assert.Equal("Sessione 1", session.Title);

        await using var context = _factory.CreateContext(folder);
        var campaign = await context.Campaigns.SingleAsync();
        Assert.Equal(session.Id, campaign.ActiveSessionId);
        Assert.Equal(1, await context.Sessions.CountAsync());
    }

    [Fact]
    public async Task StartSession_SecondSession_IncrementsNumber()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.StartSessionAsync(folder);

        var session = await service.StartSessionAsync(folder);

        Assert.Equal(2, session.Number);
        Assert.Equal("Sessione 2", session.Title);
    }

    [Fact]
    public async Task StartSession_WithCustomTitle_UsesIt()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var session = await service.StartSessionAsync(folder, "La Caduta di Harken");

        Assert.Equal("La Caduta di Harken", session.Title);
    }

    [Fact]
    public async Task StartSession_RecordsStartedEvent_WithTimestamp()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var session = await service.StartSessionAsync(folder);

        await using var context = _factory.CreateContext(folder);
        var evt = await context.SessionEvents.SingleAsync();

        Assert.Equal(SessionEventType.SessionStarted, evt.Type);
        Assert.Equal(session.Id, evt.SessionId);
        Assert.Equal(session.StartedAt, evt.Timestamp);
    }

    // ---------- Get active ----------

    [Fact]
    public async Task GetActiveSession_WithoutActive_ReturnsNull()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var session = await service.GetActiveSessionAsync(folder);

        Assert.Null(session);
    }

    [Fact]
    public async Task GetActiveSession_ReturnsActive()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var started = await service.StartSessionAsync(folder);

        var active = await service.GetActiveSessionAsync(folder);

        Assert.NotNull(active);
        Assert.Equal(started.Id, active!.Id);
        Assert.Equal(started.Number, active.Number);
        Assert.True(active.IsActive);
    }

    [Fact]
    public async Task GetActiveSession_PersistsAcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.StartSessionAsync(folder);

        var active = await service.GetActiveSessionAsync(folder);

        Assert.NotNull(active);
    }

    // ---------- Close ----------

    [Fact]
    public async Task CloseSession_EndsActive_AndClearsCampaign()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.StartSessionAsync(folder);

        var closed = await service.CloseSessionAsync(folder);

        Assert.False(closed.IsActive);
        Assert.NotNull(closed.EndedAt);
        Assert.True(closed.EndedAt >= closed.StartedAt);

        await using var context = _factory.CreateContext(folder);
        var campaign = await context.Campaigns.SingleAsync();
        Assert.Null(campaign.ActiveSessionId);
        Assert.False((await context.Sessions.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task CloseSession_WithoutActive_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<SessionException>(() => service.CloseSessionAsync(folder));
    }

    [Fact]
    public async Task CloseSession_RecordsEndedEvent()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.StartSessionAsync(folder);

        var closed = await service.CloseSessionAsync(folder);

        await using var context = _factory.CreateContext(folder);
        var evt = await context.SessionEvents
            .OrderByDescending(e => e.Timestamp)
            .FirstAsync(e => e.Type == SessionEventType.SessionEnded);

        Assert.Equal(closed.Id, evt.SessionId);
        Assert.Equal(closed.EndedAt, evt.Timestamp);
    }

    // ---------- Reopen ----------

    [Fact]
    public async Task OpenSession_ReactivatesEndedSession()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var started = await service.StartSessionAsync(folder);
        await service.CloseSessionAsync(folder);

        var reopened = await service.OpenSessionAsync(folder, started.Id);

        Assert.True(reopened.IsActive);
        Assert.Null(reopened.EndedAt);
        Assert.Equal(started.Id, reopened.Id);

        await using var context = _factory.CreateContext(folder);
        var campaign = await context.Campaigns.SingleAsync();
        Assert.Equal(started.Id, campaign.ActiveSessionId);
    }

    [Fact]
    public async Task OpenSession_MissingSession_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<SessionException>(() => service.OpenSessionAsync(folder, Guid.NewGuid()));
    }

    [Fact]
    public async Task OpenSession_ClosesOtherActiveSessions()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var s1 = await service.StartSessionAsync(folder);
        await service.CloseSessionAsync(folder);
        await service.StartSessionAsync(folder);
        await service.CloseSessionAsync(folder);

        var reopened = await service.OpenSessionAsync(folder, s1.Id);

        await using var context = _factory.CreateContext(folder);
        var active = await context.Sessions.CountAsync(s => s.IsActive);
        Assert.Equal(1, active);
        Assert.Equal(reopened.Id, await context.Sessions.Where(s => s.IsActive).Select(s => s.Id).SingleAsync());
    }

    // ---------- Autosave / persistence ----------

    [Fact]
    public async Task SessionState_AutosavesImmediately_AcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var started = await service.StartSessionAsync(folder);

        // New context reads the persisted state without any explicit save.
        await using var context = _factory.CreateContext(folder);
        var stored = await context.Sessions.SingleAsync(s => s.Id == started.Id);
        Assert.True(stored.IsActive);
        Assert.Equal(1, stored.Number);
        Assert.Equal(started.StartedAt, stored.StartedAt);
    }

    [Fact]
    public async Task QuickNote_AutosavesAndIsListed()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.StartSessionAsync(folder);

        var note = await service.AddQuickNoteAsync(folder, "Il gruppo entra nella taverna.");

        Assert.Equal("Il gruppo entra nella taverna.", note.Content);

        var notes = await service.ListQuickNotesAsync(folder);
        var loaded = Assert.Single(notes);
        Assert.Equal(note.Id, loaded.Id);
        Assert.Equal(note.Content, loaded.Content);
    }

    [Fact]
    public async Task QuickNote_WithoutActiveSession_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<SessionException>(
            () => service.AddQuickNoteAsync(folder, "nota"));
    }

    [Fact]
    public async Task QuickNote_EmptyContent_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.StartSessionAsync(folder);

        await Assert.ThrowsAsync<SessionException>(
            () => service.AddQuickNoteAsync(folder, "  "));
    }

    [Fact]
    public async Task QuickNotes_AreOrderedNewestFirst()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.StartSessionAsync(folder);

        await service.AddQuickNoteAsync(folder, "Prima");
        await service.AddQuickNoteAsync(folder, "Seconda");

        var notes = await service.ListQuickNotesAsync(folder);
        Assert.Equal("Seconda", notes[0].Content);
        Assert.Equal("Prima", notes[1].Content);
    }

    // ---------- Events ----------

    [Fact]
    public async Task AddEvent_IsStoredAndListed()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.StartSessionAsync(folder);

        var evt = await service.AddEventAsync(folder, SessionEventType.SceneEntered, "Entrano nella torre");

        Assert.Equal(SessionEventType.SceneEntered, evt.Type);
        Assert.Equal("Entrano nella torre", evt.Description);

        var events = await service.ListRecentEventsAsync(folder);
        Assert.Contains(events, e => e.Id == evt.Id);
    }

    [Fact]
    public async Task AddEvent_WithoutActiveSession_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<SessionException>(
            () => service.AddEventAsync(folder, SessionEventType.Other, "x"));
    }

    [Fact]
    public async Task AddEvent_EmptyDescription_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.StartSessionAsync(folder);

        await Assert.ThrowsAsync<SessionException>(
            () => service.AddEventAsync(folder, SessionEventType.Other, "  "));
    }

    [Fact]
    public async Task Events_OrderedNewestFirst_AndLimited()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.StartSessionAsync(folder);

        for (var i = 0; i < 5; i++)
        {
            await service.AddEventAsync(folder, SessionEventType.Other, $"Evento {i}");
        }

        var events = await service.ListRecentEventsAsync(folder, limit: 3);

        Assert.Equal(3, events.Count);
        Assert.Equal("Evento 4", events[0].Description);
    }

    // ---------- List sessions ----------

    [Fact]
    public async Task ListSessions_ReturnsAll_NewestFirst()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var s1 = await service.StartSessionAsync(folder);
        await service.CloseSessionAsync(folder);
        var s2 = await service.StartSessionAsync(folder);
        await service.CloseSessionAsync(folder);

        var sessions = await service.ListSessionsAsync(folder);

        Assert.Equal(2, sessions.Count);
        Assert.Equal(s2.Number, sessions[0].Number);
        Assert.Equal(s1.Number, sessions[1].Number);
    }

    [Fact]
    public async Task SessionInfo_IncludesEventAndNoteCounts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var session = await service.StartSessionAsync(folder);
        await service.AddQuickNoteAsync(folder, "Nota");
        await service.AddEventAsync(folder, SessionEventType.Other, "Evento");

        var info = await service.GetActiveSessionAsync(folder);

        Assert.NotNull(info);
        Assert.Equal(session.Id, info!.Id);
        Assert.Equal(3, info.EventCount); // SessionStarted + NoteAdded + Evento
        Assert.Equal(1, info.QuickNoteCount);
    }
}