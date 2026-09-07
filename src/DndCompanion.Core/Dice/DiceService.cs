using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Dice;

/// <summary>
/// Default <see cref="IDiceService"/> implementation. It parses dice notation
/// and performs rolls with a shared calculator. This class has no dependency on
/// the UI or on persistence.
/// </summary>
public sealed class DiceService : IDiceService
{
    private readonly DiceRollCalculator _calculator;

    public DiceService()
        : this(new DiceRollCalculator())
    {
    }

    public DiceService(DiceRollCalculator calculator)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    public IReadOnlyList<DiceType> SupportedDice { get; } = new[]
    {
        DiceType.D4, DiceType.D6, DiceType.D8, DiceType.D10, DiceType.D12, DiceType.D20, DiceType.D100
    };

    public DiceRollResult Roll(string notation, DiceRollMode mode = DiceRollMode.Normal)
    {
        return _calculator.Roll(DiceParser.Parse(notation), mode);
    }

    public DiceRollResult Roll(DiceExpression expression, DiceRollMode mode = DiceRollMode.Normal)
    {
        return _calculator.Roll(expression, mode);
    }

    public DiceRollResult RollPhysical(string notation, int physicalValue)
    {
        return _calculator.RollPhysical(DiceParser.Parse(notation), physicalValue);
    }

    public DiceRollResult RollPhysical(DiceExpression expression, int physicalValue)
    {
        return _calculator.RollPhysical(expression, physicalValue);
    }

    public DiceExpression Parse(string notation)
    {
        return DiceParser.Parse(notation);
    }

    public bool TryParse(string? notation, out DiceExpression? expression)
    {
        return DiceParser.TryParse(notation, out expression);
    }
}