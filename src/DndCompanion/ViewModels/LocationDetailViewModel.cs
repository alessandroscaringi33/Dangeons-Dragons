using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Locations;
using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Quests;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the location detail screen. Raw fields are editable; the
/// linked quests are shown as read-only context.
/// </summary>
public sealed partial class LocationDetailViewModel : ViewModelBase
{
    private readonly ILocationService _locationService;
    private readonly IQuestService _questService;
    private readonly ILogger<LocationDetailViewModel> _logger;

    private Guid _locationId;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<QuestItemViewModel> linkedQuests = new();

    [ObservableProperty]
    private bool hasLinkedQuests;

    public LocationDetailViewModel(
        ILocationService locationService,
        IQuestService questService,
        ILogger<LocationDetailViewModel> logger)
    {
        _locationService = locationService;
        _questService = questService;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool HasNoLinkedQuests => !HasLinkedQuests;

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));
    partial void OnHasLinkedQuestsChanged(bool value) => OnPropertyChanged(nameof(HasNoLinkedQuests));

    public void SetLocationId(Guid locationId) => _locationId = locationId;

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
            var location = await _locationService.GetLocationAsync(FolderPath, _locationId);
            Name = location.Name;
            Description = location.Description;
            Notes = location.Notes;

            var quests = await _questService.ListQuestsAsync(FolderPath);
            LinkedQuests.Clear();
            foreach (var quest in quests.Where(q => q.LocationId == _locationId))
            {
                LinkedQuests.Add(new QuestItemViewModel(quest));
            }

            HasLinkedQuests = LinkedQuests.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load location {Location} in {Folder}", _locationId, FolderPath);
            StatusMessage = "Impossibile caricare il luogo.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Il nome del luogo è obbligatorio.";
            return;
        }

        await ExecuteAsync(
            () => _locationService.UpdateLocationAsync(FolderPath, _locationId, Name, Description, Notes),
            "Luogo salvato.",
            "Impossibile salvare il luogo.");
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
        }
        catch (LocationException ex)
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
}