namespace DndCompanion.Core.Locations;

/// <summary>UI-facing description of a campaign location.</summary>
public sealed class LocationInfo
{
    public Guid Id { get; init; }

    public Guid CampaignId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    /// <summary>Whether the location has been linked to at least one quest.</summary>
    public int QuestCount { get; init; }
}