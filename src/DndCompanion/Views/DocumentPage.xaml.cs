using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
public partial class DocumentPage : ContentPage
{
    private readonly DocumentViewModel _viewModel;

    public DocumentPage(DocumentViewModel viewModel)
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

    private async void OnReadPdfClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: PdfItemViewModel item })
        {
            await _viewModel.ImportAsync(item);
        }
    }

    private async void OnStoryClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(StoryPage),
            new Dictionary<string, object>
            {
                ["FolderPath"] = _viewModel.FolderPath,
                ["CampaignName"] = _viewModel.CampaignName
            });
    }
}