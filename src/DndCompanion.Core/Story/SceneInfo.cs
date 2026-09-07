namespace DndCompanion.Core.Story;

/// <summary>
/// Lightweight, UI-facing description of a scene belonging to a chapter.
/// </summary>
public sealed class SceneInfo
{
    public Guid Id { get; init; }

    public Guid ChapterId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int Order { get; init; }

    public bool IsCompleted { get; init; }

    public string Notes { get; init; } = string.Empty;
}