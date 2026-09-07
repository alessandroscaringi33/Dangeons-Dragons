namespace DndCompanion.Core.Domain.Entities;

/// <summary>A place relevant to the campaign, such as a tavern, dungeon or city.</summary>
public class Location : Entity
{
    public Guid CampaignId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}
