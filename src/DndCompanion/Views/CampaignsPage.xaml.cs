using DndCompanion.ViewModels;

namespace DndCompanion.Views;

public partial class CampaignsPage : ContentPage
{
    private readonly CampaignsViewModel _viewModel;

    public CampaignsPage(CampaignsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnNewCampaignClicked(object? sender, EventArgs e)
    {
        var name = await DisplayPromptAsync(
            "Nuova campagna",
            "Inserisci il nome della campagna:",
            "Crea",
            "Annulla",
            maxLength: 100);

        if (!string.IsNullOrWhiteSpace(name))
        {
            await _viewModel.CreateCampaignAsync(name);
        }
    }

    private async void OnOpenCampaignClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CampaignItemViewModel item })
        {
            var info = await _viewModel.OpenAsync(item);
            if (info is not null)
            {
                await Shell.Current.GoToAsync(
                    nameof(SessionPage),
                    new Dictionary<string, object>
                    {
                        ["FolderPath"] = info.FolderPath,
                        ["CampaignName"] = info.Name
                    });
            }
        }
    }
}