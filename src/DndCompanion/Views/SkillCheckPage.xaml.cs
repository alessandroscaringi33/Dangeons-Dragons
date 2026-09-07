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

    private async void OnSessionClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(SessionPage),
            new Dictionary<string, object>
            {
                ["FolderPath"] = _viewModel.FolderPath,
                ["CampaignName"] = _viewModel.CampaignName
            });
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