namespace DndCompanion.Core.Domain.Enums;

/// <summary>How a <see cref="Entities.DiceRoll"/> was performed.</summary>
public enum DiceRollMode
{
    /// <summary>A single straight roll.</summary>
    Normal,

    /// <summary>Roll twice, keep the highest result.</summary>
    Advantage,

    /// <summary>Roll twice, keep the lowest result.</summary>
    Disadvantage,

    /// <summary>The result was entered manually (physical roll).</summary>
    Manual
}
