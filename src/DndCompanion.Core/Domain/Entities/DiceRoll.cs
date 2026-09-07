using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// A single dice roll recorded during a session. The total is always derived
/// from the individual results plus the modifier, so it can never be
/// inconsistent with the rolled values.
/// </summary>
public class DiceRoll : Entity
{
    public Guid SessionId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Human readable notation, e.g. <c>2d6 + 3</c>.</summary>
    public string DiceNotation { get; set; } = string.Empty;

    public DiceType DiceType { get; set; } = DiceType.D20;

    public int DiceCount { get; set; } = 1;

    public int Modifier { get; set; }

    public List<int> Results { get; set; } = new();

    public DiceRollMode RollMode { get; set; } = DiceRollMode.Normal;

    public string Purpose { get; set; } = string.Empty;

    public Guid? CharacterId { get; set; }

    /// <summary>True when the player physically rolled the dice and the value
    /// was typed into the app instead of being generated.</summary>
    public bool IsPhysicalRoll { get; set; }

    /// <summary>Sum of all rolled values plus the modifier.</summary>
    public int Total => Results.Sum() + Modifier;

    /// <summary>The best single result (used by advantage).</summary>
    public int BestResult => Results.Count == 0 ? 0 : Results.Max();

    /// <summary>The worst single result (used by disadvantage).</summary>
    public int WorstResult => Results.Count == 0 ? 0 : Results.Min();
}
