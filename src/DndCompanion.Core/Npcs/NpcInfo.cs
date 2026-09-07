namespace DndCompanion.Core.Npcs;

/// <summary>
/// UI-facing description of a non-player character, including its optional
/// scene and location links.
/// </summary>
public sealed class NpcInfo
{
    public Guid Id { get; init; }

    public Guid CampaignId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int CurrentHp { get; init; }

    public int MaxHp { get; init; }

    public int ArmorClass { get; init; }

    public int InitiativeModifier { get; init; }

    public string Notes { get; init; } = string.Empty;

    public bool IsAlive { get; init; } = true;

    public bool IsKnown { get; init; }

    /// <summary>Identifier of the linked scene, if any.</summary>
    public Guid? SceneId { get; init; }

    /// <summary>Title of the linked scene, if any.</summary>
    public string? SceneTitle { get; init; }

    /// <summary>Identifier of the linked location, if any.</summary>
    public Guid? LocationId { get; init; }

    /// <summary>Name of the linked location, if any.</summary>
    public string? LocationName { get; init; }
}