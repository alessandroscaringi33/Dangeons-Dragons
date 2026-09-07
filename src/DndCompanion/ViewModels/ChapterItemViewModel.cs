using System.Collections.ObjectModel;
using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Story;

namespace DndCompanion.ViewModels;

/// <summary>
/// Presentation model of a single chapter with its nested scenes for the
/// story tree.
/// </summary>
public sealed class ChapterItemViewModel : ViewModelBase
{
    public ChapterItemViewModel(ChapterInfo chapter)
    {
        Chapter = chapter;
        Scenes = new ObservableCollection<SceneItemViewModel>(
            chapter.Scenes.Select(s => new SceneItemViewModel(s, s.Id == chapter.CurrentSceneId)));
    }

    public ChapterInfo Chapter { get; }

    public Guid Id => Chapter.Id;

    public string Title => Chapter.Title;

    public string Description => Chapter.Description;

    public int Order => Chapter.Order;

    public string SceneCountText => Chapter.Scenes.Count == 0
        ? "Nessuna scena"
        : Chapter.Scenes.Count == 1
            ? "1 scena"
            : $"{Chapter.Scenes.Count} scene";

    public ObservableCollection<SceneItemViewModel> Scenes { get; }

    public bool CanMoveUp => Order > 1;

    public bool CanMoveDown => true;
}