namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// A free-form note. Belongs to a campaign and may optionally be attached
/// to a specific session.
/// </summary>
public class Note : Entity
{
    public Guid CampaignId { get; set; }

    public Guid? SessionId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsPinned { get; set; }

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}
