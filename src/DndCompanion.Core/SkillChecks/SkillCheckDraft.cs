namespace DndCompanion.Core.SkillChecks;

/// <summary>
/// Input for a new skill check. When <see cref="IsPhysicalRoll"/> is true the
/// <see cref="Roll"/> is used verbatim; otherwise it is generated from the dice
/// engine using <see cref="DiceNotation"/>.
/// </summary>
public sealed class SkillCheckDraft
{
    /// <summary>Character performing the check, if any.</summary>
    public Guid? CharacterId { get; set; }

    public string Skill { get; set; } = string.Empty;

    /// <summary>Dice notation used when the roll is generated, e.g. <c>1d20</c>.</summary>
    public string DiceNotation { get; set; } = "1d20";

    public int Modifier { get; set; }

    public int DifficultyClass { get; set; }

    /// <summary>The physically rolled value when <see cref="IsPhysicalRoll"/> is true.</summary>
    public int? PhysicalRoll { get; set; }

    public bool IsPhysicalRoll { get; set; }
}