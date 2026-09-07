using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Locations;
using DndCompanion.Core.Mvvm;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the campaign locations management screen.
/// </summary>
public sealed partial class LocationsViewModel : ViewModelBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationsViewModel> _logger;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LocationItemViewModel> locations = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasLocations;

    public LocationsViewModel(
        ILocationService locationService,
        ILogger<LocationsViewModel> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool HasNoLocations => !HasLocations;

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));
    partial void OnHasLocationsChanged(bool value) => OnPropertyChanged(nameof(HasNoLocations));

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
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load locations of campaign in {Folder}", FolderPath);
            StatusMessage = "Impossibile caricare i luoghi.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CreateLocationAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await ExecuteAsync(
            () => _locationService.CreateLocationAsync(FolderPath, name, string.Empty),
            $"Luogo '{name.Trim()}' creato.",
            "Impossibile creare il luogo.");
    }

    public async Task UpdateLocationAsync(LocationItemViewModel? location, string name)
    {
        if (location is null || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await ExecuteAsync(
            () => _locationService.UpdateLocationAsync(FolderPath, location.Id, name, location.Description),
            $"Luogo '{name.Trim()}' aggiornato.",
            "Impossibile aggiornare il luogo.");
    }

    public async Task DeleteLocationAsync(LocationItemViewModel? location)
    {
        if (location is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _locationService.DeleteLocationAsync(FolderPath, location.Id),
            $"Luogo '{location.Name}' eliminato.",
            "Impossibile eliminare il luogo.");
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

    private async Task ReloadAsync()
    {
        var locations = await _locationService.ListLocationsAsync(FolderPath);
        Locations.Clear();
        foreach (var location in locations)
        {
            Locations.Add(new LocationItemViewModel(location));
        }

        HasLocations = Locations.Count > 0;
    }
}