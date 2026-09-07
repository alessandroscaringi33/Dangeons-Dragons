namespace DndCompanion.Core.Dice;

/// <summary>
/// Thrown when a dice notation string cannot be parsed into a valid dice
/// expression.
/// </summary>
public sealed class DiceParseException : Exception
{
    public DiceParseException(string message)
        : base(message)
    {
    }
}