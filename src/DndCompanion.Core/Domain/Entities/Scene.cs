namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// A scene is a single playable moment of a chapter. The DM advances the
/// story by moving from one scene to the next.
/// </summary>
public class Scene : Entity
{
    public Guid ChapterId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Position of the scene within its chapter.</summary>
    public int Order { get; set; }

    public bool IsCompleted { get; set; }

    public string Notes { get; set; } = string.Empty;

    public void Complete() => IsCompleted = true;

    public void Reopen() => IsCompleted = false;
}
