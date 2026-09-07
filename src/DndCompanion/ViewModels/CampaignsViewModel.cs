using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Campaigns;
using DndCompanion.Core.Mvvm;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

public sealed partial class CampaignsViewModel : ViewModelBase
{
    private readonly ICampaignService _campaignService;
    private readonly ILogger<CampaignsViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<CampaignItemViewModel> campaigns = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string campaignsPath = string.Empty;

    public CampaignsViewModel(ICampaignService campaignService, ILogger<CampaignsViewModel> logger)
    {
        _campaignService = campaignService;
        _logger = logger;
        CampaignsPath = campaignService.CampaignsFolderPath;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

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
            var items = await _campaignService.ListCampaignsAsync();
            Campaigns.Clear();
            foreach (var info in items)
            {
                Campaigns.Add(new CampaignItemViewModel(info));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load campaigns");
            StatusMessage = "Impossibile caricare le campagne.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<CampaignInfo?> OpenAsync(CampaignItemViewModel? item)
    {
        if (item is null)
        {
            return null;
        }

        try
        {
            var info = await _campaignService.OpenCampaignAsync(item.Name);

            var index = Campaigns.IndexOf(item);
            if (index >= 0)
            {
                Campaigns[index] = new CampaignItemViewModel(info);
            }

            StatusMessage = $"Campagna '{info.Name}' aperta.";
            _logger.LogInformation("Opened campaign '{Name}'", info.Name);
            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open campaign '{Name}'", item.Name);
            StatusMessage = $"Impossibile aprire la campagna '{item.Name}'.";
            return null;
        }
    }

    public async Task CreateCampaignAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            var info = await _campaignService.CreateCampaignAsync(name);
            Campaigns.Add(new CampaignItemViewModel(info));
            StatusMessage = $"Campagna '{info.Name}' creata.";
            _logger.LogInformation("Created campaign '{Name}'", info.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create campaign '{Name}'", name);
            StatusMessage = $"Impossibile creare la campagna '{name}'.";
        }
    }
}