using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
public partial class NpcsPage : ContentPage
{
    private readonly NpcsViewModel _viewModel;

    public NpcsPage(NpcsViewModel viewModel)
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

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        // Debounce-less: search triggers from the search button / return key.
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        await _viewModel.SearchAsync(SearchBarEntry.Text);
    }

    private async void OnNewNpcClicked(object? sender, EventArgs e)
    {
        var name = await DisplayPromptAsync(
            "Nuovo NPC",
            "Inserisci il nome dell'NPC:",
            "Crea",
            "Annulla",
            maxLength: 200);

        if (!string.IsNullOrWhiteSpace(name))
        {
            await _viewModel.CreateNpcAsync(name);
        }
    }

    private async void OnOpenClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: NpcItemViewModel item })
        {
            await Shell.Current.GoToAsync(
                nameof(NpcDetailPage),
                new Dictionary<string, object>
                {
                    ["FolderPath"] = _viewModel.FolderPath,
                    ["CampaignName"] = _viewModel.CampaignName,
                    ["NpcId"] = item.Id
                });
        }
    }

    private async void OnDamageClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: NpcItemViewModel item })
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
        if (sender is Button { CommandParameter: NpcItemViewModel item })
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
        if (sender is Button { CommandParameter: NpcItemViewModel item })
        {
            var amount = await PromptAmountAsync("Imposta HP", $"HP di '{item.Name}' (max {item.Npc.MaxHp}):");
            if (amount >= 0)
            {
                await _viewModel.SetCurrentHpAsync(item, amount);
            }
        }
    }

    private async void OnToggleAliveClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: NpcItemViewModel item })
        {
            await _viewModel.ToggleAliveAsync(item);
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: NpcItemViewModel item })
        {
            var confirmed = await DisplayAlert(
                "Elimina NPC",
                $"Eliminare l'NPC '{item.Name}'?",
                "Elimina",
                "Annulla");

            if (confirmed)
            {
                await _viewModel.DeleteNpcAsync(item);
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