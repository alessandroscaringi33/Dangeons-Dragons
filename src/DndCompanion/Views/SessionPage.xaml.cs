using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
public partial class SessionPage : ContentPage
{
    private readonly SessionViewModel _viewModel;

    public SessionPage(SessionViewModel viewModel)
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

    private async void OnStartSessionClicked(object? sender, EventArgs e)
    {
        await _viewModel.StartSessionAsync();
    }

    private async void OnCloseSessionClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlert(
            "Chiudi sessione",
            "Chiudere la sessione corrente?",
            "Chiudi",
            "Annulla");

        if (confirmed)
        {
            await _viewModel.CloseSessionAsync();
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await _viewModel.RefreshAsync();
    }

    private async void OnCharacterDamageClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CharacterItemViewModel item })
        {
            var amount = ParseAmount(item.QuickAmount);
            if (amount > 0)
            {
                await _viewModel.ApplyCharacterDamageAsync(item, amount);
            }
        }
    }

    private async void OnCharacterHealClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CharacterItemViewModel item })
        {
            var amount = ParseAmount(item.QuickAmount);
            if (amount > 0)
            {
                await _viewModel.HealCharacterAsync(item, amount);
            }
        }
    }

    private async void OnNpcDamageClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: NpcItemViewModel item })
        {
            var amount = ParseAmount(item.QuickAmount);
            if (amount > 0)
            {
                await _viewModel.ApplyNpcDamageAsync(item, amount);
            }
        }
    }

    private async void OnNpcHealClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: NpcItemViewModel item })
        {
            var amount = ParseAmount(item.QuickAmount);
            if (amount > 0)
            {
                await _viewModel.HealNpcAsync(item, amount);
            }
        }
    }

    private async void OnAddQuickNoteClicked(object? sender, EventArgs e)
    {
        var content = QuickNoteEntry.Text;
        if (!string.IsNullOrWhiteSpace(content))
        {
            await _viewModel.AddQuickNoteAsync(content);
            QuickNoteEntry.Text = string.Empty;
        }
    }

    private async void OnTiroClicked(object? sender, EventArgs e)
    {
        await GoToAsync(nameof(SkillCheckPage));
    }

    private async void OnCombatClicked(object? sender, EventArgs e)
    {
        await DisplayAlert(
            "Combattimento",
            "Il combattimento sarà disponibile in una fase successiva.",
            "Ok");
    }

    private async void OnCharactersClicked(object? sender, EventArgs e)
    {
        await GoToAsync(nameof(CharactersPage));
    }

    private async void OnNpcsClicked(object? sender, EventArgs e)
    {
        await GoToAsync(nameof(NpcsPage));
    }

    private async void OnStoryClicked(object? sender, EventArgs e)
    {
        await GoToAsync(nameof(StoryPage));
    }

    private async Task GoToAsync(string page)
    {
        await Shell.Current.GoToAsync(
            page,
            new Dictionary<string, object>
            {
                ["FolderPath"] = _viewModel.FolderPath,
                ["CampaignName"] = _viewModel.CampaignName
            });
    }

    private static int ParseAmount(string value)
    {
        return int.TryParse(value, out var amount) ? amount : 0;
    }
}