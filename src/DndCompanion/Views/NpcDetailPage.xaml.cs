using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
[QueryProperty(nameof(NpcId), "NpcId")]
public partial class NpcDetailPage : ContentPage
{
    private readonly NpcDetailViewModel _viewModel;

    public NpcDetailPage(NpcDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public string FolderPath
    {
        set => _viewModel.FolderPath = value;
    }

    public string CampaignName
    {
        set => _viewModel.CampaignName = value;
    }

    public string NpcId
    {
        set
        {
            if (Guid.TryParse(value, out var id))
            {
                _viewModel.SetNpcId(id);
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        await _viewModel.SaveAsync();
    }

    private async void OnDamageClicked(object? sender, EventArgs e)
    {
        var amount = ReadQuickAmount();
        if (amount > 0)
        {
            await _viewModel.ApplyDamageAsync(amount);
        }
    }

    private async void OnHealClicked(object? sender, EventArgs e)
    {
        var amount = ReadQuickAmount();
        if (amount > 0)
        {
            await _viewModel.HealAsync(amount);
        }
    }

    private async void OnSetHpClicked(object? sender, EventArgs e)
    {
        var value = await DisplayPromptAsync(
            "Imposta HP",
            "Inserisci il nuovo valore di HP correnti:",
            "Ok",
            "Annulla",
            keyboard: Keyboard.Numeric,
            maxLength: 6);

        if (int.TryParse(value, out var hp))
        {
            await _viewModel.SetCurrentHpAsync(hp);
        }
    }

    private async void OnToggleAliveClicked(object? sender, EventArgs e)
    {
        await _viewModel.ToggleAliveAsync();
    }

    private async void OnLinkSceneClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_viewModel.SelectedSceneTitle))
        {
            await _viewModel.LinkToSceneAsync(_viewModel.SelectedSceneTitle);
        }
    }

    private async void OnClearSceneClicked(object? sender, EventArgs e)
    {
        await _viewModel.ClearSceneLinkAsync();
    }

    private async void OnLinkLocationClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_viewModel.SelectedLocationName))
        {
            await _viewModel.LinkToLocationAsync(_viewModel.SelectedLocationName);
        }
    }

    private async void OnClearLocationClicked(object? sender, EventArgs e)
    {
        await _viewModel.ClearLocationLinkAsync();
    }

    private int ReadQuickAmount()
    {
        return int.TryParse(HpQuickAmount.Text, out var amount) ? amount : 0;
    }
}