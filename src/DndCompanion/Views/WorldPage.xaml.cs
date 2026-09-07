using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
public partial class WorldPage : ContentPage
{
    private readonly WorldViewModel _viewModel;

    public WorldPage(WorldViewModel viewModel)
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

    private async void OnSearchLocationsClicked(object? sender, EventArgs e)
    {
        await _viewModel.SearchLocationsAsync(LocationSearchEntry.Text);
    }

    private async void OnSearchQuestsClicked(object? sender, EventArgs e)
    {
        await _viewModel.SearchQuestsAsync(QuestSearchEntry.Text);
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

    private async void OnOpenLocationClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: LocationItemViewModel item })
        {
            await Shell.Current.GoToAsync(
                nameof(LocationDetailPage),
                new Dictionary<string, object>
                {
                    ["FolderPath"] = _viewModel.FolderPath,
                    ["CampaignName"] = _viewModel.CampaignName,
                    ["LocationId"] = item.Id
                });
        }
    }

    private async void OnDeleteLocationClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: LocationItemViewModel item })
        {
            var confirmed = await DisplayAlert(
                "Elimina luogo",
                $"Eliminare il luogo '{item.Name}'?",
                "Elimina",
                "Annulla");

            if (confirmed)
            {
                await _viewModel.DeleteLocationAsync(item);
            }
        }
    }

    private async void OnNewQuestClicked(object? sender, EventArgs e)
    {
        var title = await DisplayPromptAsync(
            "Nuova quest",
            "Inserisci il titolo della quest:",
            "Crea",
            "Annulla",
            maxLength: 300);

        if (!string.IsNullOrWhiteSpace(title))
        {
            await _viewModel.CreateQuestAsync(title);
        }
    }

    private async void OnOpenQuestClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: QuestItemViewModel item })
        {
            await Shell.Current.GoToAsync(
                nameof(QuestDetailPage),
                new Dictionary<string, object>
                {
                    ["FolderPath"] = _viewModel.FolderPath,
                    ["CampaignName"] = _viewModel.CampaignName,
                    ["QuestId"] = item.Id
                });
        }
    }

    private async void OnEditQuestClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: QuestItemViewModel item })
        {
            await Shell.Current.GoToAsync(
                nameof(QuestDetailPage),
                new Dictionary<string, object>
                {
                    ["FolderPath"] = _viewModel.FolderPath,
                    ["CampaignName"] = _viewModel.CampaignName,
                    ["QuestId"] = item.Id
                });
        }
    }

    private async void OnQuestStatusClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: QuestItemViewModel item })
        {
            var options = new[]
            {
                "NotStarted", "Active", "Completed", "Failed", "Abandoned"
            };
            var choice = await DisplayActionSheet(
                $"Stato di '{item.Title}'",
                "Annulla",
                null,
                options);
            if (choice is not null && choice != "Annulla")
            {
                await _viewModel.SetQuestStatusAsync(item, choice);
            }
        }
    }

    private async void OnDeleteQuestClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: QuestItemViewModel item })
        {
            var confirmed = await DisplayAlert(
                "Elimina quest",
                $"Eliminare la quest '{item.Title}'?",
                "Elimina",
                "Annulla");

            if (confirmed)
            {
                await _viewModel.DeleteQuestAsync(item);
            }
        }
    }
}