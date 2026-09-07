using DndCompanion.ViewModels;

namespace DndCompanion.Views;

[QueryProperty(nameof(FolderPath), "FolderPath")]
[QueryProperty(nameof(CampaignName), "CampaignName")]
public partial class StoryPage : ContentPage
{
    private readonly StoryViewModel _viewModel;

    public StoryPage(StoryViewModel viewModel)
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

    private async void OnAddChapterClicked(object? sender, EventArgs e)
    {
        var title = await DisplayPromptAsync(
            "Nuovo capitolo",
            "Inserisci il titolo del capitolo:",
            "Crea",
            "Annulla",
            maxLength: 300);

        if (!string.IsNullOrWhiteSpace(title))
        {
            await _viewModel.CreateChapterAsync(title);
        }
    }

    private async void OnEditChapterClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ChapterItemViewModel chapter })
        {
            var title = await DisplayPromptAsync(
                "Modifica capitolo",
                "Inserisci il nuovo titolo:",
                "Salva",
                "Annulla",
                initialValue: chapter.Title,
                maxLength: 300);

            if (!string.IsNullOrWhiteSpace(title))
            {
                await _viewModel.UpdateChapterAsync(chapter, title);
            }
        }
    }

    private async void OnDeleteChapterClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ChapterItemViewModel chapter })
        {
            var confirmed = await DisplayAlert(
                "Elimina capitolo",
                $"Eliminare il capitolo '{chapter.Title}' e tutte le sue scene?",
                "Elimina",
                "Annulla");

            if (confirmed)
            {
                await _viewModel.DeleteChapterAsync(chapter);
            }
        }
    }

    private async void OnMoveChapterUpClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ChapterItemViewModel chapter })
        {
            await _viewModel.MoveChapterAsync(chapter, up: true);
        }
    }

    private async void OnMoveChapterDownClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ChapterItemViewModel chapter })
        {
            await _viewModel.MoveChapterAsync(chapter, up: false);
        }
    }

    private async void OnAddSceneClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ChapterItemViewModel chapter })
        {
            var title = await DisplayPromptAsync(
                "Nuova scena",
                "Inserisci il titolo della scena:",
                "Crea",
                "Annulla",
                maxLength: 300);

            if (!string.IsNullOrWhiteSpace(title))
            {
                await _viewModel.CreateSceneAsync(chapter, title);
            }
        }
    }

    private async void OnEditSceneClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: SceneItemViewModel scene })
        {
            var title = await DisplayPromptAsync(
                "Modifica scena",
                "Inserisci il nuovo titolo:",
                "Salva",
                "Annulla",
                initialValue: scene.Title,
                maxLength: 300);

            if (!string.IsNullOrWhiteSpace(title))
            {
                await _viewModel.UpdateSceneAsync(scene, title);
            }
        }
    }

    private async void OnDeleteSceneClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: SceneItemViewModel scene })
        {
            var confirmed = await DisplayAlert(
                "Elimina scena",
                $"Eliminare la scena '{scene.Title}'?",
                "Elimina",
                "Annulla");

            if (confirmed)
            {
                await _viewModel.DeleteSceneAsync(scene);
            }
        }
    }

    private async void OnMoveSceneUpClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: SceneItemViewModel scene })
        {
            await _viewModel.MoveSceneAsync(scene, up: true);
        }
    }

    private async void OnMoveSceneDownClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: SceneItemViewModel scene })
        {
            await _viewModel.MoveSceneAsync(scene, up: false);
        }
    }

    private async void OnCompleteSceneClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: SceneItemViewModel scene })
        {
            await _viewModel.CompleteSceneAsync(scene);
        }
    }

    private async void OnSetCurrentSceneClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: SceneItemViewModel scene })
        {
            await _viewModel.SetCurrentSceneAsync(scene);
        }
    }
}