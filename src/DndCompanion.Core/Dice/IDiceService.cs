using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Dice;

/// <summary>
/// High-level dice engine facade used by the rest of the application.
/// Fully independent from the UI.
/// </summary>
public interface IDiceService
{
    /// <summary>Rolls the given notation in normal mode.</summary>
    DiceRollResult Roll(string notation, DiceRollMode mode = DiceRollMode.Normal);

    /// <summary>Rolls the given expression in the given mode.</summary>
    DiceRollResult Roll(DiceExpression expression, DiceRollMode mode = DiceRollMode.Normal);

    /// <summary>Registers a physical roll: the provided value is used as the
    /// real result and no random value is generated.</summary>
    DiceRollResult RollPhysical(string notation, int physicalValue);

    /// <summary>Registers a physical roll on an already parsed expression.</summary>
    DiceRollResult RollPhysical(DiceExpression expression, int physicalValue);

    /// <summary>Parses a dice notation string.</summary>
    DiceExpression Parse(string notation);

    /// <summary>Attempts to parse a notation string without throwing.</summary>
    bool TryParse(string? notation, out DiceExpression? expression);

    /// <summary>The supported dice denominations.</summary>
    IReadOnlyList<DiceType> SupportedDice { get; }
}