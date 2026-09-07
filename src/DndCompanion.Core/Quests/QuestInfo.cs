using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Quests;

/// <summary>
/// UI-facing description of a quest including its linked chapter, scene,
/// location and NPC.
/// </summary>
public sealed class QuestInfo
{
    public Guid Id { get; init; }

    public Guid CampaignId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public QuestStatus Status { get; init; }

    public string Notes { get; init; } = string.Empty;

    /// <summary>Identifier of the linked chapter, if any.</summary>
    public Guid? ChapterId { get; init; }

    /// <summary>Title of the linked chapter, if any.</summary>
    public string? ChapterTitle { get; init; }

    /// <summary>Identifier of the linked scene, if any.</summary>
    public Guid? SceneId { get; init; }

    /// <summary>Title of the linked scene, if any.</summary>
    public string? SceneTitle { get; init; }

    /// <summary>Identifier of the linked location, if any.</summary>
    public Guid? LocationId { get; init; }

    /// <summary>Name of the linked location, if any.</summary>
    public string? LocationName { get; init; }

    /// <summary>Identifier of the linked NPC, if any.</summary>
    public Guid? NpcId { get; init; }

    /// <summary>Name of the linked NPC, if any.</summary>
    public string? NpcName { get; init; }
}