namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// A chapter organizes the story of a campaign and contains an ordered
/// collection of scenes.
/// </summary>
public class Chapter : Entity
{
    public Guid CampaignId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Position of the chapter within its campaign.</summary>
    public int Order { get; set; }

    public string Notes { get; set; } = string.Empty;

    public List<Scene> Scenes { get; set; } = new();
}
