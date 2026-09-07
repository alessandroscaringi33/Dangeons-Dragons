using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.Locations;
using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Npcs;
using DndCompanion.Core.Quests;
using DndCompanion.Core.Story;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the quest detail screen: editable fields, status, notes and
/// links to chapter, scene, location and NPC.
/// </summary>
public sealed partial class QuestDetailViewModel : ViewModelBase
{
    private readonly IQuestService _questService;
    private readonly IChapterService _chapterService;
    private readonly ILocationService _locationService;
    private readonly INpcService _npcService;
    private readonly ILogger<QuestDetailViewModel> _logger;

    private Guid _questId;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string selectedStatus = "NotStarted";

    [ObservableProperty]
    private string selectedChapter = string.Empty;

    [ObservableProperty]
    private string selectedScene = string.Empty;

    [ObservableProperty]
    private string selectedLocation = string.Empty;

    [ObservableProperty]
    private string selectedNpc = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> statuses = new()
    {
        "NotStarted", "Active", "Completed", "Failed", "Abandoned"
    };

    [ObservableProperty]
    private ObservableCollection<string> chapters = new();

    [ObservableProperty]
    private ObservableCollection<string> scenes = new();

    [ObservableProperty]
    private ObservableCollection<string> locations = new();

    [ObservableProperty]
    private ObservableCollection<string> npcs = new();

    private readonly Dictionary<string, Guid> _chapterLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid> _sceneLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid> _locationLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid> _npcLookup = new(StringComparer.OrdinalIgnoreCase);

    public QuestDetailViewModel(
        IQuestService questService,
        IChapterService chapterService,
        ILocationService locationService,
        INpcService npcService,
        ILogger<QuestDetailViewModel> logger)
    {
        _questService = questService;
        _chapterService = chapterService;
        _locationService = locationService;
        _npcService = npcService;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

    public void SetQuestId(Guid questId) => _questId = questId;

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
            await LoadOptionsAsync();
            var quest = await _questService.GetQuestAsync(FolderPath, _questId);
            Apply(quest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load quest {Quest} in {Folder}", _questId, FolderPath);
            StatusMessage = "Impossibile caricare la quest.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            StatusMessage = "Il titolo della quest è obbligatorio.";
            return;
        }

        var draft = new QuestDraft
        {
            Title = Title,
            Description = Description,
            Status = ParseStatus(SelectedStatus),
            Notes = Notes,
            ChapterId = Lookup(_chapterLookup, SelectedChapter),
            SceneId = Lookup(_sceneLookup, SelectedScene),
            LocationId = Lookup(_locationLookup, SelectedLocation),
            NpcId = Lookup(_npcLookup, SelectedNpc)
        };

        await ExecuteAsync(
            () => _questService.UpdateQuestAsync(FolderPath, _questId, draft),
            "Quest salvata.",
            "Impossibile salvare la quest.");
    }

    private async Task LoadOptionsAsync()
    {
        _chapterLookup.Clear();
        Chapters.Clear();
        Chapters.Add(string.Empty);
        var chapterList = await _chapterService.ListChaptersAsync(FolderPath);
        foreach (var chapter in chapterList)
        {
            Chapters.Add(chapter.Title);
            _chapterLookup[chapter.Title] = chapter.Id;
            foreach (var scene in chapter.Scenes)
            {
                Scenes.Add(scene.Title);
                _sceneLookup[scene.Title] = scene.Id;
            }
        }

        _locationLookup.Clear();
        Locations.Clear();
        Locations.Add(string.Empty);
        var locationList = await _locationService.ListLocationsAsync(FolderPath);
        foreach (var location in locationList)
        {
            Locations.Add(location.Name);
            _locationLookup[location.Name] = location.Id;
        }

        _npcLookup.Clear();
        Npcs.Clear();
        Npcs.Add(string.Empty);
        var npcList = await _npcService.ListNpcsAsync(FolderPath);
        foreach (var npc in npcList)
        {
            Npcs.Add(npc.Name);
            _npcLookup[npc.Name] = npc.Id;
        }
    }

    private void Apply(QuestInfo quest)
    {
        Title = quest.Title;
        Description = quest.Description;
        Notes = quest.Notes;
        SelectedStatus = quest.Status.ToString();
        SelectedChapter = quest.ChapterTitle ?? string.Empty;
        SelectedScene = quest.SceneTitle ?? string.Empty;
        SelectedLocation = quest.LocationName ?? string.Empty;
        SelectedNpc = quest.NpcName ?? string.Empty;
    }

    private async Task ExecuteAsync(Func<Task> action, string? successMessage, string errorMessage)
    {
        try
        {
            IsLoading = true;
            await action();

            if (successMessage is not null)
            {
                StatusMessage = successMessage;
            }

            await ReloadAsync();
        }
        catch (QuestException ex)
        {
            _logger.LogWarning(ex, "{Message}", ex.Message);
            StatusMessage = ex.Message;
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

    private async Task ReloadAsync()
    {
        try
        {
            var quest = await _questService.GetQuestAsync(FolderPath, _questId);
            Apply(quest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload quest {Quest}", _questId);
        }
    }

    private static Guid? Lookup(Dictionary<string, Guid> lookup, string value)
    {
        return !string.IsNullOrEmpty(value) && lookup.TryGetValue(value, out var id) ? id : null;
    }

    private static QuestStatus ParseStatus(string value)
    {
        foreach (var status in Enum.GetValues<QuestStatus>())
        {
            if (status.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return status;
            }
        }

        return QuestStatus.NotStarted;
    }
}