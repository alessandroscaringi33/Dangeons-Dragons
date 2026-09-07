using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Dice;

/// <summary>
/// The outcome of a dice roll: the individual results, how the roll was made
/// and the derived totals. Independent from the UI.
/// </summary>
public sealed class DiceRollResult
{
    public DiceRollResult(
        string notation,
        DiceType diceType,
        int diceCount,
        int modifier,
        DiceRollMode mode,
        IReadOnlyList<int> results,
        int keptResult,
        bool isPhysicalRoll)
    {
        Notation = notation;
        DiceType = diceType;
        DiceCount = diceCount;
        Modifier = modifier;
        Mode = mode;
        Results = results;
        KeptResult = keptResult;
        IsPhysicalRoll = isPhysicalRoll;
    }

    public string Notation { get; }

    public DiceType DiceType { get; }

    public int DiceCount { get; }

    public int Modifier { get; }

    public DiceRollMode Mode { get; }

    /// <summary>The individual die results. For advantage/disadvantage this is
    /// the full set of rolled values (both attempts concatenated).</summary>
    public IReadOnlyList<int> Results { get; }

    /// <summary>True when the DM typed a physical roll instead of a generated one.</summary>
    public bool IsPhysicalRoll { get; }

    /// <summary>The outcome actually used: the sum of the dice (normal), the
    /// higher of the two attempts (advantage), the lower (disadvantage), or the
    /// typed value (physical roll).</summary>
    public int KeptResult { get; }

    /// <summary>Sum of every individual rolled value.</summary>
    public int RawSum => Results.Sum();

    /// <summary>Total = kept outcome + modifier.</summary>
    public int Total => KeptResult + Modifier;

    /// <summary>The best single die result.</summary>
    public int BestDie => Results.Count == 0 ? 0 : Results.Max();

    /// <summary>The worst single die result.</summary>
    public int WorstDie => Results.Count == 0 ? 0 : Results.Min();

    public bool IsAdvantage => Mode == DiceRollMode.Advantage;

    public bool IsDisadvantage => Mode == DiceRollMode.Disadvantage;
}