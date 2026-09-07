using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Dice;

/// <summary>
/// A parsed dice expression, e.g. <c>2d6 + 3</c>, describing the dice count,
/// the dice denomination and a flat modifier. Immutable and independent from
/// the UI.
/// </summary>
public sealed class DiceExpression
{
    public DiceExpression(int diceCount, DiceType diceType, int modifier)
    {
        DiceCount = diceCount;
        DiceType = diceType;
        Modifier = modifier;
    }

    /// <summary>Number of dice to roll.</summary>
    public int DiceCount { get; }

    /// <summary>Denomination of the dice.</summary>
    public DiceType DiceType { get; }

    /// <summary>Flat modifier added to the total.</summary>
    public int Modifier { get; }

    /// <summary>The number of faces of the die.</summary>
    public int Faces => (int)DiceType;

    /// <summary>Canonical notation, e.g. <c>2d6+3</c>.</summary>
    public string Notation => $"{DiceCount}d{Faces}{(Modifier == 0 ? string.Empty : Modifier > 0 ? $"+{Modifier}" : Modifier.ToString())}";

    /// <summary>Minimum possible total (all ones plus the modifier).</summary>
    public int MinTotal => DiceCount + Modifier;

    /// <summary>Maximum possible total (all max faces plus the modifier).</summary>
    public int MaxTotal => DiceCount * Faces + Modifier;

    /// <summary>Average total.</summary>
    public double AverageTotal => (MinTotal + MaxTotal) / 2d;
}