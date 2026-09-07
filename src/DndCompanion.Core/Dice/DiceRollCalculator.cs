using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Dice;

/// <summary>
/// Performs dice rolls according to the standard D&amp;D rules. Supports normal
/// rolls, advantage, disadvantage and physical rolls. It does not depend on the
/// UI and never falsifies a physical roll.
/// </summary>
public sealed class DiceRollCalculator
{
    private readonly Random _random;

    public DiceRollCalculator()
        : this(new Random())
    {
    }

    /// <param name="random">Injected randomness source for testability.</param>
    public DiceRollCalculator(Random random)
    {
        _random = random ?? new Random();
    }

    /// <summary>
    /// Rolls the given expression with the given mode.
    /// </summary>
    public DiceRollResult Roll(DiceExpression expression, DiceRollMode mode = DiceRollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(expression);

        if (mode is DiceRollMode.Advantage or DiceRollMode.Disadvantage)
        {
            // Roll the whole set twice and keep the higher/lower group total.
            var first = RollOnce(expression.DiceCount, expression.Faces);
            var second = RollOnce(expression.DiceCount, expression.Faces);

            var firstTotal = first.Sum();
            var secondTotal = second.Sum();

            var kept = mode == DiceRollMode.Advantage
                ? Math.Max(firstTotal, secondTotal)
                : Math.Min(firstTotal, secondTotal);

            var results = new List<int>(first);
            results.AddRange(second);

            return new DiceRollResult(
                expression.Notation,
                expression.DiceType,
                expression.DiceCount,
                expression.Modifier,
                mode,
                results,
                kept,
                isPhysicalRoll: false);
        }

        var rolled = RollOnce(expression.DiceCount, expression.Faces);
        return new DiceRollResult(
            expression.Notation,
            expression.DiceType,
            expression.DiceCount,
            expression.Modifier,
            DiceRollMode.Normal,
            rolled,
            rolled.Sum(),
            isPhysicalRoll: false);
    }

    /// <summary>
    /// Rolls the given expression in manual mode with an explicit single result.
    /// Used for a physical roll: the provided value is used as the real result
    /// and no random value is generated.
    /// </summary>
    /// <exception cref="ArgumentException">When the physical value is out of the die range.</exception>
    public DiceRollResult RollPhysical(DiceExpression expression, int physicalValue)
    {
        ArgumentNullException.ThrowIfNull(expression);

        if (expression.DiceCount != 1)
        {
            throw new ArgumentException("Un tiro fisico supporta un singolo dado per volta.", nameof(expression));
        }

        if (physicalValue < 1 || physicalValue > expression.Faces)
        {
            throw new ArgumentOutOfRangeException(
                nameof(physicalValue),
                $"Il valore fisico {physicalValue} non è compreso tra 1 e {expression.Faces}.");
        }

        return new DiceRollResult(
            expression.Notation,
            expression.DiceType,
            expression.DiceCount,
            expression.Modifier,
            DiceRollMode.Manual,
            new[] { physicalValue },
            physicalValue,
            isPhysicalRoll: true);
    }

    /// <summary>
    /// Convenience: parses the notation and rolls in one step.
    /// </summary>
    public DiceRollResult Roll(string notation, DiceRollMode mode = DiceRollMode.Normal)
    {
        return Roll(DiceParser.Parse(notation), mode);
    }

    /// <summary>
    /// Convenience: parses the notation and performs a physical roll.
    /// </summary>
    public DiceRollResult RollPhysical(string notation, int physicalValue)
    {
        return RollPhysical(DiceParser.Parse(notation), physicalValue);
    }

    private int[] RollOnce(int count, int faces)
    {
        var results = new int[count];
        for (var i = 0; i < count; i++)
        {
            results[i] = _random.Next(1, faces + 1);
        }

        return results;
    }
}