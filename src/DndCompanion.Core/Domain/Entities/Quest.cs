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

    public void Start() => Status = QuestStatus.Active;

    public void Complete() => Status = QuestStatus.Completed;

    public void Fail() => Status = QuestStatus.Failed;

    public void Abandon() => Status = QuestStatus.Abandoned;
}
