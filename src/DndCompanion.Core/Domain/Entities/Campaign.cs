namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// Root aggregate of the domain. A campaign groups every other entity:
/// story, characters, NPCs, locations, quests, sessions and notes.
/// </summary>
public class Campaign : Entity
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Path of the source PDF associated with the campaign.</summary>
    public string SourcePdfPath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Identifies the scene currently focused by the DM.</summary>
    public Guid? CurrentSceneId { get; set; }

    /// <summary>Identifies the session currently in progress, if any.</summary>
    public Guid? ActiveSessionId { get; set; }

    public List<Chapter> Chapters { get; set; } = new();

    public List<Character> Characters { get; set; } = new();

    public List<Npc> Npcs { get; set; } = new();

    public List<Location> Locations { get; set; } = new();

    public List<Quest> Quests { get; set; } = new();

    public List<Session> Sessions { get; set; } = new();

    public List<Note> Notes { get; set; } = new();

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}
