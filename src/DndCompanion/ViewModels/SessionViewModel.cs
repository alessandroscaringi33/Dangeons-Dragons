using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Characters;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Npcs;
using DndCompanion.Core.Quests;
using DndCompanion.Core.Sessions;
using DndCompanion.Core.SkillChecks;
using DndCompanion.Core.Story;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the in-game Session hub. Shows the campaign, the current
/// session, the current scene, characters with HP, active quests, important
/// NPCs, the last roll, recent events and quick notes, with a persistent
/// action bar. Optimized for use during a real game with minimal clicks.
/// </summary>
public sealed partial class SessionViewModel : ViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly IChapterService _chapterService;
    private readonly ICharacterService _characterService;
    private readonly INpcService _npcService;
    private readonly IQuestService _questService;
    private readonly ISkillCheckService _skillCheckService;
    private readonly ILogger<SessionViewModel> _logger;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    // Session header
    [ObservableProperty]
    private bool hasActiveSession;

    [ObservableProperty]
    private string sessionTitle = "Nessuna sessione attiva";

    [ObservableProperty]
    private string sessionSubtitle = string.Empty;

    // Current scene
    [ObservableProperty]
    private string currentSceneText = "Nessuna scena corrente";

    // Last roll
    [ObservableProperty]
    private string lastRollText = "Nessun tiro";

    [ObservableProperty]
    private bool hasLastRoll;

    // Panels
    [ObservableProperty]
    private ObservableCollection<CharacterItemViewModel> characters = new();

    [ObservableProperty]
    private ObservableCollection<QuestItemViewModel> activeQuests = new();

    [ObservableProperty]
    private ObservableCollection<NpcItemViewModel> importantNpcs = new();

    [ObservableProperty]
    private ObservableCollection<SessionEventItemViewModel> recentEvents = new();

    [ObservableProperty]
    private ObservableCollection<QuickNoteItemViewModel> quickNotes = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SessionViewModel(
        ISessionService sessionService,
        IChapterService chapterService,
        ICharacterService characterService,
        INpcService npcService,
        IQuestService questService,
        ISkillCheckService skillCheckService,
        ILogger<SessionViewModel> logger)
    {
        _sessionService = sessionService;
        _chapterService = chapterService;
        _characterService = characterService;
        _npcService = npcService;
        _questService = questService;
        _skillCheckService = skillCheckService;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool HasNoActiveSession => !HasActiveSession;

    public bool HasNoCharacters => Characters.Count == 0;

    public bool HasNoActiveQuests => ActiveQuests.Count == 0;

    public bool HasNoImportantNpcs => ImportantNpcs.Count == 0;

    public bool HasNoRecentEvents => RecentEvents.Count == 0;

    public bool HasNoQuickNotes => QuickNotes.Count == 0;

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

    partial void OnHasActiveSessionChanged(bool value) => OnPropertyChanged(nameof(HasNoActiveSession));

    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            await LoadSessionAsync();
            await LoadSceneAsync();
            await LoadCharactersAsync();
            await LoadQuestsAsync();
            await LoadNpcsAsync();
            await LoadLastRollAsync();
            await LoadEventsAsync();
            await LoadNotesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session hub for {Folder}", FolderPath);
            StatusMessage = "Impossibile caricare la sessione.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshAsync() => await LoadAsync();

    public async Task StartSessionAsync()
    {
        await ExecuteAsync(
            () => _sessionService.StartSessionAsync(FolderPath),
            null,
            "Impossibile avviare la sessione.");
    }

    public async Task CloseSessionAsync()
    {
        await ExecuteAsync(
            () => _sessionService.CloseSessionAsync(FolderPath),
            "Sessione chiusa.",
            "Impossibile chiudere la sessione.");
    }

    public async Task AddQuickNoteAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        await ExecuteAsync(
            () => _sessionService.AddQuickNoteAsync(FolderPath, content),
            "Nota aggiunta.",
            "Impossibile aggiungere la nota.",
            reloadNotes: true);
    }

    public async Task ApplyCharacterDamageAsync(CharacterItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteAsync(
            () => _characterService.ApplyDamageAsync(FolderPath, item.Id, amount),
            null,
            "Impossibile applicare il danno.",
            reloadCharacters: true);
    }

    public async Task HealCharacterAsync(CharacterItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteAsync(
            () => _characterService.HealAsync(FolderPath, item.Id, amount),
            null,
            "Impossibile curare il personaggio.",
            reloadCharacters: true);
    }

    public async Task ApplyNpcDamageAsync(NpcItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.ApplyDamageAsync(FolderPath, item.Id, amount),
            null,
            "Impossibile applicare il danno.",
            reloadNpcs: true);
    }

    public async Task HealNpcAsync(NpcItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.HealAsync(FolderPath, item.Id, amount),
            null,
            "Impossibile curare l'NPC.",
            reloadNpcs: true);
    }

    private async Task LoadSessionAsync()
    {
        var session = await _sessionService.GetActiveSessionAsync(FolderPath);
        HasActiveSession = session is not null;

        if (session is null)
        {
            SessionTitle = "Nessuna sessione attiva";
            SessionSubtitle = "Avvia una nuova sessione per iniziare il gioco.";
            return;
        }

        SessionTitle = session.Title;
        var started = session.StartedAt.ToLocalTime().ToString("HH:mm");
        SessionSubtitle = $"Sessione {session.Number} · iniziata alle {started} · {session.EventCount} eventi · {session.QuickNoteCount} note";
    }

    private async Task LoadSceneAsync()
    {
        var chapters = await _chapterService.ListChaptersAsync(FolderPath);
        var currentSceneId = chapters.FirstOrDefault(c => c.CurrentSceneId is not null)?.CurrentSceneId;

        if (currentSceneId is Guid sceneId)
        {
            var match = chapters
                .SelectMany(c => c.Scenes.Select(s => new { Chapter = c.Title, Scene = s }))
                .FirstOrDefault(x => x.Scene.Id == sceneId);

            if (match is not null)
            {
                CurrentSceneText = $"{match.Scene.Title} — {match.Chapter}";
                return;
            }
        }

        CurrentSceneText = "Nessuna scena corrente";
    }

    private async Task LoadCharactersAsync()
    {
        var characters = await _characterService.ListCharactersAsync(FolderPath);
        Characters.Clear();
        foreach (var character in characters)
        {
            Characters.Add(new CharacterItemViewModel(character));
        }

        OnPropertyChanged(nameof(HasNoCharacters));
    }

    private async Task LoadQuestsAsync()
    {
        var quests = await _questService.ListQuestsAsync(FolderPath);
        ActiveQuests.Clear();
        foreach (var quest in quests.Where(q => q.Status == QuestStatus.Active))
        {
            ActiveQuests.Add(new QuestItemViewModel(quest));
        }

        OnPropertyChanged(nameof(HasNoActiveQuests));
    }

    private async Task LoadNpcsAsync()
    {
        var npcs = await _npcService.ListNpcsAsync(FolderPath);
        ImportantNpcs.Clear();
        // Important = known or alive NPCs, limited for a compact panel.
        foreach (var npc in npcs.Where(n => n.IsKnown || !n.IsAlive).Take(6))
        {
            ImportantNpcs.Add(new NpcItemViewModel(npc));
        }

        OnPropertyChanged(nameof(HasNoImportantNpcs));
    }

    private async Task LoadLastRollAsync()
    {
        var checks = await _skillCheckService.ListChecksAsync(FolderPath, limit: 1);
        var last = checks.FirstOrDefault();

        if (last is null)
        {
            LastRollText = "Nessun tiro";
            HasLastRoll = false;
            return;
        }

        var who = string.IsNullOrWhiteSpace(last.CharacterName) ? "Gruppo" : last.CharacterName;
        LastRollText = $"{who} — {last.Skill}: {last.Total} ({last.OutcomeText}, CD {last.DifficultyClass})";
        HasLastRoll = true;
    }

    private async Task LoadEventsAsync()
    {
        var events = await _sessionService.ListRecentEventsAsync(FolderPath, limit: 5);
        RecentEvents.Clear();
        foreach (var evt in events)
        {
            RecentEvents.Add(new SessionEventItemViewModel(evt));
        }

        OnPropertyChanged(nameof(HasNoRecentEvents));
    }

    private async Task LoadNotesAsync()
    {
        var notes = await _sessionService.ListQuickNotesAsync(FolderPath, limit: 5);
        QuickNotes.Clear();
        foreach (var note in notes)
        {
            QuickNotes.Add(new QuickNoteItemViewModel(note));
        }

        OnPropertyChanged(nameof(HasNoQuickNotes));
    }

    private async Task ExecuteAsync(
        Func<Task> action,
        string? successMessage,
        string errorMessage,
        bool reloadCharacters = false,
        bool reloadNpcs = false,
        bool reloadNotes = false)
    {
        try
        {
            IsLoading = true;
            await action();

            if (successMessage is not null)
            {
                StatusMessage = successMessage;
            }

            await LoadSessionAsync();
            if (reloadCharacters)
            {
                await LoadCharactersAsync();
            }

            if (reloadNpcs)
            {
                await LoadNpcsAsync();
            }

            if (reloadNotes)
            {
                await LoadNotesAsync();
                await LoadEventsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", errorMessage);
            StatusMessage = errorMessage;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>Presentation model of a session timeline event.</summary>
public sealed class SessionEventItemViewModel : ViewModelBase
{
    public SessionEventItemViewModel(SessionEventInfo evt)
    {
        Event = evt;
    }

    public SessionEventInfo Event { get; }

    public string TimeText => Event.Timestamp.ToLocalTime().ToString("HH:mm");

    public string Description => Event.Description;

    public string TypeText => Event.Type switch
    {
        SessionEventType.SessionStarted => "Inizio sessione",
        SessionEventType.SessionEnded => "Fine sessione",
        SessionEventType.SceneEntered => "Scena",
        SessionEventType.NpcMet => "NPC incontrato",
        SessionEventType.NpcDefeated => "NPC sconfitto",
        SessionEventType.CombatStarted => "Combattimento",
        SessionEventType.CombatEnded => "Fine combattimento",
        SessionEventType.DiceRolled => "Tiro",
        SessionEventType.SkillCheck => "Skill check",
        SessionEventType.QuestStarted => "Quest",
        SessionEventType.QuestProgressed => "Quest avanzata",
        SessionEventType.QuestCompleted => "Quest completata",
        SessionEventType.QuestFailed => "Quest fallita",
        SessionEventType.LocationVisited => "Luogo",
        SessionEventType.NoteAdded => "Nota",
        _ => "Evento"
    };
}

/// <summary>Presentation model of a quick note.</summary>
public sealed class QuickNoteItemViewModel : ViewModelBase
{
    public QuickNoteItemViewModel(QuickNoteInfo note)
    {
        Note = note;
    }

    public QuickNoteInfo Note { get; }

    public string Content => Note.Content;

    public string TimeText => Note.Timestamp.ToLocalTime().ToString("HH:mm");
}