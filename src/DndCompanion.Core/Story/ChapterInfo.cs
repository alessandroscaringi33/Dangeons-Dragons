namespace DndCompanion.Core.Story;

/// <summary>
/// Lightweight, UI-facing description of a chapter of a campaign including
/// its ordered scenes. Used to render the story tree.
/// </summary>
public sealed class ChapterInfo
{
    public Guid Id { get; init; }

    public Guid CampaignId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int Order { get; init; }

    public string Notes { get; init; } = string.Empty;

    public IReadOnlyList<SceneInfo> Scenes { get; init; } = Array.Empty<SceneInfo>();

    /// <summary>Identifier of the scene the DM currently focuses, if any.</summary>
    public Guid? CurrentSceneId { get; init; }
}