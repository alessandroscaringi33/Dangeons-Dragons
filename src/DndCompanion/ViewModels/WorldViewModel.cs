using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.Locations;
using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Quests;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the "Mondo" screen: a DM hub with a Luoghi section and a
/// Quest section (list, detail, edit, search, status, notes).
/// </summary>
public sealed partial class WorldViewModel : ViewModelBase
{
    private readonly ILocationService _locationService;
    private readonly IQuestService _questService;
    private readonly ILogger<WorldViewModel> _logger;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LocationItemViewModel> locations = new();

    [ObservableProperty]
    private ObservableCollection<QuestItemViewModel> quests = new();

    [ObservableProperty]
    private string locationSearchText = string.Empty;

    [ObservableProperty]
    private string questSearchText = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasLocations;

    [ObservableProperty]
    private bool hasQuests;

    public WorldViewModel(
        ILocationService locationService,
        IQuestService questService,
        ILogger<WorldViewModel> logger)
    {
        _locationService = locationService;
        _questService = questService;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool HasNoLocations => !HasLocations;

    public bool HasNoQuests => !HasQuests;

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));
    partial void OnHasLocationsChanged(bool value) => OnPropertyChanged(nameof(HasNoLocations));
    partial void OnHasQuestsChanged(bool value) => OnPropertyChanged(nameof(HasNoQuests));

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
            await LoadLocationsAsync();
            await LoadQuestsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the world of campaign in {Folder}", FolderPath);
            StatusMessage = "Impossibile caricare il mondo della campagna.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SearchLocationsAsync(string searchText)
    {
        LocationSearchText = searchText ?? string.Empty;
        await LoadLocationsAsync();
    }

    public async Task SearchQuestsAsync(string searchText)
    {
        QuestSearchText = searchText ?? string.Empty;
        await LoadQuestsAsync();
    }

    public async Task CreateLocationAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await ExecuteAsync(
            () => _locationService.CreateLocationAsync(FolderPath, name, string.Empty, string.Empty),
            $"Luogo '{name.Trim()}' creato.",
            "Impossibile creare il luogo.",
            LoadLocationsAsync);
    }

    public async Task DeleteLocationAsync(LocationItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _locationService.DeleteLocationAsync(FolderPath, item.Id),
            $"Luogo '{item.Name}' eliminato.",
            "Impossibile eliminare il luogo.",
            LoadLocationsAsync);
    }

    public async Task CreateQuestAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        await ExecuteAsync(
            () => _questService.CreateQuestAsync(FolderPath, new QuestDraft { Title = title }),
            $"Quest '{title.Trim()}' creata.",
            "Impossibile creare la quest.",
            LoadQuestsAsync);
    }

    public async Task DeleteQuestAsync(QuestItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _questService.DeleteQuestAsync(FolderPath, item.Id),
            $"Quest '{item.Title}' eliminata.",
            "Impossibile eliminare la quest.",
            LoadQuestsAsync);
    }

    public async Task SetQuestStatusAsync(QuestItemViewModel? item, string statusName)
    {
        if (item is null)
        {
            return;
        }

        if (!TryParseStatus(statusName, out var status))
        {
            return;
        }

        await ExecuteAsync(
            () => _questService.SetStatusAsync(FolderPath, item.Id, status),
            null,
            "Impossibile aggiornare lo stato.",
            LoadQuestsAsync);
    }

    private async Task LoadLocationsAsync()
    {
        var locations = await _locationService.ListLocationsAsync(FolderPath, LocationSearchText);
        Locations.Clear();
        foreach (var location in locations)
        {
            Locations.Add(new LocationItemViewModel(location));
        }

        HasLocations = Locations.Count > 0;
    }

    private async Task LoadQuestsAsync()
    {
        var quests = await _questService.ListQuestsAsync(FolderPath, QuestSearchText);
        Quests.Clear();
        foreach (var quest in quests)
        {
            Quests.Add(new QuestItemViewModel(quest));
        }

        HasQuests = Quests.Count > 0;
    }

    private async Task ExecuteAsync(
        Func<Task> action,
        string? successMessage,
        string errorMessage,
        Func<Task> reload)
    {
        try
        {
            IsLoading = true;
            await action();

            if (successMessage is not null)
            {
                StatusMessage = successMessage;
            }

            await reload();
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

    private static bool TryParseStatus(string name, out QuestStatus status)
    {
        foreach (var value in Enum.GetValues<QuestStatus>())
        {
            if (value.ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                status = value;
                return true;
            }
        }

        status = QuestStatus.NotStarted;
        return false;
    }
}