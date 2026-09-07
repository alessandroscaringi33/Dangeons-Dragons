using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Story;

namespace DndCompanion.ViewModels;

/// <summary>
/// Presentation model of a single scene within a chapter of the story tree.
/// </summary>
public sealed class SceneItemViewModel : ViewModelBase
{
    private static readonly Color CurrentColor = Color.FromArgb("#B08D57");
    private static readonly Color CompletedStatusColor = Color.FromArgb("#4CAF50");
    private static readonly Color DefaultColor = Color.FromArgb("#7A7A7A");

    private static readonly Color CurrentBackgroundColor = Color.FromArgb("#2A2218");
    private static readonly Color CurrentStrokeColor = Color.FromArgb("#B08D57");
    private static readonly Color NormalBackgroundColor = Color.FromArgb("#161616");
    private static readonly Color NormalStrokeColor = Color.FromArgb("#2E2E2E");

    public SceneItemViewModel(SceneInfo scene, bool isCurrent)
    {
        Scene = scene;
        IsCurrent = isCurrent;
    }

    public SceneInfo Scene { get; }

    public Guid Id => Scene.Id;

    public Guid ChapterId => Scene.ChapterId;

    public string Title => Scene.Title;

    public string Description => Scene.Description;

    public int Order => Scene.Order;

    public bool IsCompleted => Scene.IsCompleted;

    public bool IsCurrent { get; }

    public bool IsNotCurrent => !IsCurrent;

    public bool IsNotCompleted => !IsCompleted;

    public bool CanMoveUp => Order > 1;

    public string CompletedText => IsCompleted ? "Completata" : "Da fare";

    public Color CompletedColor => IsCompleted ? CompletedStatusColor : DefaultColor;

    public Color TitleColor => IsCurrent ? CurrentColor : Color.FromArgb("#E8E8E8");

    public Color BackgroundColor => IsCurrent ? CurrentBackgroundColor : NormalBackgroundColor;

    public Color StrokeColor => IsCurrent ? CurrentStrokeColor : NormalStrokeColor;

    public double StrokeThickness => IsCurrent ? 2 : 1;
}