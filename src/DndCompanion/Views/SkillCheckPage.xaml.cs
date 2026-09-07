using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
public partial class SkillCheckPage : ContentPage
{
    private readonly SkillCheckViewModel _viewModel;

    public SkillCheckPage(SkillCheckViewModel viewModel)
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

    private async void OnRollClicked(object? sender, EventArgs e)
    {
        await _viewModel.RollAsync();
    }
}