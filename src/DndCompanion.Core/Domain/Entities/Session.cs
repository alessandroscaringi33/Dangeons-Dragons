namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// A single play session of a campaign. The session is the operational centre
/// during the game and records events, dice rolls and combats.
/// </summary>
public class Session : Entity
{
    public Guid CampaignId { get; set; }

    /// <summary>Progressive number of the session within the campaign.</summary>
    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<SessionEvent> Events { get; set; } = new();

    public List<DiceRoll> DiceRolls { get; set; } = new();

    public List<Combat> Combats { get; set; } = new();

    public List<Note> SessionNotes { get; set; } = new();

    public void Start()
    {
        IsActive = true;
        StartedAt = DateTime.UtcNow;
        EndedAt = null;
    }

    public void End()
    {
        IsActive = false;
        EndedAt = DateTime.UtcNow;
    }
}
