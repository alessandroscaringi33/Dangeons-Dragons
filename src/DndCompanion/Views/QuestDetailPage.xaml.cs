using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
[QueryProperty(nameof(QuestId), "QuestId")]
public partial class QuestDetailPage : ContentPage
{
    private readonly QuestDetailViewModel _viewModel;

    public QuestDetailPage(QuestDetailViewModel viewModel)
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

    public string QuestId
    {
        set
        {
            if (Guid.TryParse(value, out var id))
            {
                _viewModel.SetQuestId(id);
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
}