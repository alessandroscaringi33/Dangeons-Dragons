using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Domain.Entities;

/// <summary>A mission or objective given to the party.</summary>
public class Quest : Entity
{
    public Guid CampaignId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public QuestStatus Status { get; set; } = QuestStatus.NotStarted;

    public string Notes { get; set; } = string.Empty;

    /// <summary>Optional chapter the quest belongs to.</summary>
    public Guid? ChapterId { get; set; }

    /// <summary>Optional scene the quest is linked to.</summary>
    public Guid? SceneId { get; set; }

    /// <summary>Optional location the quest is linked to.</summary>
    public Guid? LocationId { get; set; }

    /// <summary>Optional NPC that gives the quest.</summary>
    public Guid? NpcId { get; set; }

    /// <summary>Navigation to the linked chapter, if any.</summary>
    public Chapter? Chapter { get; set; }

    /// <summary>Navigation to the linked scene, if any.</summary>
    public Scene? Scene { get; set; }

    /// <summary>Navigation to the linked location, if any.</summary>
    public Location? Location { get; set; }

    /// <summary>Navigation to the linked NPC, if any.</summary>
    public Npc? Npc { get; set; }

    public void Start() => Status = QuestStatus.Active;

    public void Complete() => Status = QuestStatus.Completed;

    public void Fail() => Status = QuestStatus.Failed;

    public void Abandon() => Status = QuestStatus.Abandoned;
}
