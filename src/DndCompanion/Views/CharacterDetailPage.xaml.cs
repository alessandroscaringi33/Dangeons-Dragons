using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
[QueryProperty(nameof(CharacterId), "CharacterId")]
public partial class CharacterDetailPage : ContentPage
{
    private readonly CharacterDetailViewModel _viewModel;

    public CharacterDetailPage(CharacterDetailViewModel viewModel)
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

    public string CharacterId
    {
        set
        {
            if (Guid.TryParse(value, out var id))
            {
                _viewModel.SetCharacterId(id);
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

    private async void OnSetTempHpClicked(object? sender, EventArgs e)
    {
        var value = await DisplayPromptAsync(
            "Imposta HP temporanei",
            "Inserisci il nuovo valore di HP temporanei:",
            "Ok",
            "Annulla",
            keyboard: Keyboard.Numeric,
            maxLength: 6);

        if (int.TryParse(value, out var hp))
        {
            await _viewModel.SetTemporaryHpAsync(hp);
        }
    }

    private async void OnConditionClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ConditionItemViewModel condition })
        {
            await _viewModel.ToggleConditionAsync(condition.Name);
        }
    }

    private async void OnAddItemClicked(object? sender, EventArgs e)
    {
        var name = NewItemEntry.Text;
        if (!string.IsNullOrWhiteSpace(name))
        {
            await _viewModel.AddInventoryItemAsync(name);
            NewItemEntry.Text = string.Empty;
        }
    }

    private async void OnRemoveItemClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: DndCompanion.Core.Characters.InventoryItemInfo item })
        {
            await _viewModel.RemoveInventoryItemAsync(item);
        }
    }

    private int ReadQuickAmount()
    {
        return int.TryParse(HpQuickAmount.Text, out var amount) ? amount : 0;
    }
}