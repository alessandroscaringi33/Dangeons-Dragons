using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Story;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the "Storia" screen: shows the campaign as a tree of
/// chapters and scenes and drives all chapter/scene CRUD and ordering.
/// Dialog prompts and confirmations are handled by the page code-behind.
/// </summary>
public sealed partial class StoryViewModel : ViewModelBase
{
    private readonly IChapterService _chapterService;
    private readonly ISceneService _sceneService;
    private readonly ILogger<StoryViewModel> _logger;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ChapterItemViewModel> chapters = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasStory;

    public StoryViewModel(
        IChapterService chapterService,
        ISceneService sceneService,
        ILogger<StoryViewModel> logger)
    {
        _chapterService = chapterService;
        _sceneService = sceneService;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool HasNoStory => !HasStory;

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

    partial void OnHasStoryChanged(bool value) => OnPropertyChanged(nameof(HasNoStory));

    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var chapters = await _chapterService.ListChaptersAsync(FolderPath);
            RebuildTree(chapters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the story of campaign in {Folder}", FolderPath);
            StatusMessage = "Impossibile caricare la storia della campagna.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CreateChapterAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        await ExecuteAsync(
            () => _chapterService.CreateChapterAsync(FolderPath, title, string.Empty),
            $"Capitolo '{title.Trim()}' creato.",
            "Impossibile creare il capitolo.");
    }

    public async Task UpdateChapterAsync(ChapterItemViewModel? chapter, string title)
    {
        if (chapter is null || string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        await ExecuteAsync(
            () => _chapterService.UpdateChapterAsync(FolderPath, chapter.Id, title, chapter.Description),
            $"Capitolo '{title.Trim()}' aggiornato.",
            "Impossibile aggiornare il capitolo.");
    }

    public async Task DeleteChapterAsync(ChapterItemViewModel? chapter)
    {
        if (chapter is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _chapterService.DeleteChapterAsync(FolderPath, chapter.Id),
            $"Capitolo '{chapter.Title}' eliminato.",
            "Impossibile eliminare il capitolo.");
    }

    public async Task MoveChapterAsync(ChapterItemViewModel? chapter, bool up)
    {
        if (chapter is null)
        {
            return;
        }

        var newOrder = chapter.Order + (up ? -1 : 1);
        await ExecuteAsync(
            () => _chapterService.MoveChapterAsync(FolderPath, chapter.Id, newOrder),
            null,
            "Impossibile spostare il capitolo.");
    }

    public async Task CreateSceneAsync(ChapterItemViewModel? chapter, string title)
    {
        if (chapter is null || string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        await ExecuteAsync(
            () => _sceneService.CreateSceneAsync(FolderPath, chapter.Id, title, string.Empty),
            $"Scena '{title.Trim()}' creata.",
            "Impossibile creare la scena.");
    }

    public async Task UpdateSceneAsync(SceneItemViewModel? scene, string title)
    {
        if (scene is null || string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        await ExecuteAsync(
            () => _sceneService.UpdateSceneAsync(FolderPath, scene.Id, title, scene.Description),
            $"Scena '{title.Trim()}' aggiornata.",
            "Impossibile aggiornare la scena.");
    }

    public async Task DeleteSceneAsync(SceneItemViewModel? scene)
    {
        if (scene is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _sceneService.DeleteSceneAsync(FolderPath, scene.Id),
            $"Scena '{scene.Title}' eliminata.",
            "Impossibile eliminare la scena.");
    }

    public async Task MoveSceneAsync(SceneItemViewModel? scene, bool up)
    {
        if (scene is null)
        {
            return;
        }

        var newOrder = scene.Order + (up ? -1 : 1);
        await ExecuteAsync(
            () => _sceneService.MoveSceneAsync(FolderPath, scene.Id, scene.ChapterId, newOrder),
            null,
            "Impossibile spostare la scena.");
    }

    public async Task CompleteSceneAsync(SceneItemViewModel? scene)
    {
        if (scene is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _sceneService.CompleteSceneAsync(FolderPath, scene.Id),
            $"Scena '{scene.Title}' completata.",
            "Impossibile completare la scena.");
    }

    public async Task SetCurrentSceneAsync(SceneItemViewModel? scene)
    {
        if (scene is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _sceneService.SetCurrentSceneAsync(FolderPath, scene.Id),
            $"'{scene.Title}' impostata come scena corrente.",
            "Impossibile impostare la scena corrente.");
    }

    private void RebuildTree(IReadOnlyList<ChapterInfo> chapters)
    {
        Chapters.Clear();
        foreach (var chapter in chapters)
        {
            Chapters.Add(new ChapterItemViewModel(chapter));
        }

        HasStory = Chapters.Count > 0;
    }

    private async Task ExecuteAsync(Func<Task> action, string? successMessage, string errorMessage)
    {
        try
        {
            IsLoading = true;
            await action();

            if (successMessage is not null)
            {
                StatusMessage = successMessage;
            }

            await ReloadAsync();
        }
        catch (StoryException ex)
        {
            _logger.LogWarning(ex, "{Message}", ex.Message);
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", errorMessage);
            StatusMessage = errorMessage;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ReloadAsync()
    {
        try
        {
            var chapters = await _chapterService.ListChaptersAsync(FolderPath);
            RebuildTree(chapters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload the story in {Folder}", FolderPath);
        }
    }
}