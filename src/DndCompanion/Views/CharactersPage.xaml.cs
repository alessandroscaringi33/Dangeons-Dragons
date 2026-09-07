using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
public partial class CharactersPage : ContentPage
{
    private readonly CharactersViewModel _viewModel;

    public CharactersPage(CharactersViewModel viewModel)
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

    private async void OnNewCharacterClicked(object? sender, EventArgs e)
    {
        var name = await DisplayPromptAsync(
            "Nuovo personaggio",
            "Inserisci il nome del personaggio:",
            "Crea",
            "Annulla",
            maxLength: 200);

        if (!string.IsNullOrWhiteSpace(name))
        {
            await _viewModel.CreateCharacterAsync(name);
        }
    }

    private async void OnOpenCharacterClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CharacterItemViewModel item })
        {
            await Shell.Current.GoToAsync(
                nameof(CharacterDetailPage),
                new Dictionary<string, object>
                {
                    ["FolderPath"] = _viewModel.FolderPath,
                    ["CampaignName"] = _viewModel.CampaignName,
                    ["CharacterId"] = item.Id
                });
        }
    }

    private async void OnDamageClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CharacterItemViewModel item })
        {
            var amount = await PromptAmountAsync("Applica danno", $"Danno a '{item.Name}':");
            if (amount > 0)
            {
                await _viewModel.ApplyDamageAsync(item, amount);
            }
        }
    }

    private async void OnHealClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CharacterItemViewModel item })
        {
            var amount = await PromptAmountAsync("Cura", $"Cura per '{item.Name}':");
            if (amount > 0)
            {
                await _viewModel.HealAsync(item, amount);
            }
        }
    }

    private async void OnSetHpClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CharacterItemViewModel item })
        {
            var amount = await PromptAmountAsync("Imposta HP", $"HP di '{item.Name}' (max {item.Character.MaxHp}):");
            if (amount >= 0)
            {
                await _viewModel.SetCurrentHpAsync(item, amount);
            }
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CharacterItemViewModel item })
        {
            var confirmed = await DisplayAlert(
                "Elimina personaggio",
                $"Eliminare il personaggio '{item.Name}'?",
                "Elimina",
                "Annulla");

            if (confirmed)
            {
                await _viewModel.DeleteCharacterAsync(item);
            }
        }
    }

    private async Task<int> PromptAmountAsync(string title, string message)
    {
        var value = await DisplayPromptAsync(
            title,
            message,
            "Ok",
            "Annulla",
            keyboard: Keyboard.Numeric,
            maxLength: 6);

        return int.TryParse(value, out var amount) ? amount : -1;
    }
}