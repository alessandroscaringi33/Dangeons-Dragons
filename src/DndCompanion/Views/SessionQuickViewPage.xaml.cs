using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
public partial class SessionQuickViewPage : ContentPage
{
    private readonly SessionQuickViewViewModel _viewModel;

    public SessionQuickViewPage(SessionQuickViewViewModel viewModel)
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

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await _viewModel.LoadAsync();
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

    private async void OnNpcToggleAliveClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: NpcItemViewModel item })
        {
            await _viewModel.ToggleNpcAliveAsync(item);
        }
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

    private static int ParseAmount(string value)
    {
        return int.TryParse(value, out var amount) ? amount : 0;
    }
}