using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
public partial class LocationsPage : ContentPage
{
    private readonly LocationsViewModel _viewModel;

    public LocationsPage(LocationsViewModel viewModel)
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnNewLocationClicked(object? sender, EventArgs e)
    {
        var name = await DisplayPromptAsync(
            "Nuovo luogo",
            "Inserisci il nome del luogo:",
            "Crea",
            "Annulla",
            maxLength: 200);

        if (!string.IsNullOrWhiteSpace(name))
        {
            await _viewModel.CreateLocationAsync(name);
        }
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: LocationItemViewModel location })
        {
            var name = await DisplayPromptAsync(
                "Modifica luogo",
                "Inserisci il nuovo nome:",
                "Salva",
                "Annulla",
                initialValue: location.Name,
                maxLength: 200);

            if (!string.IsNullOrWhiteSpace(name))
            {
                await _viewModel.UpdateLocationAsync(location, name);
            }
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: LocationItemViewModel location })
        {
            var confirmed = await DisplayAlert(
                "Elimina luogo",
                $"Eliminare il luogo '{location.Name}'? Gli NPC collegati verranno scollegati.",
                "Elimina",
                "Annulla");

            if (confirmed)
            {
                await _viewModel.DeleteLocationAsync(location);
            }
        }
    }
}