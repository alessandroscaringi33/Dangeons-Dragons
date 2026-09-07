namespace DndCompanion.Core.Npcs;

/// <summary>
/// Editable profile of an NPC used to create or update it.
/// </summary>
public sealed class NpcDraft
{
    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int CurrentHp { get; set; }

    public int MaxHp { get; set; }

    public int ArmorClass { get; set; }

    public int InitiativeModifier { get; set; }

    public string Notes { get; set; } = string.Empty;

    public bool IsKnown { get; set; }

    public Guid? SceneId { get; set; }

    public Guid? LocationId { get; set; }
}