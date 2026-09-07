namespace DndCompanion.Core.Domain;

/// <summary>
/// Centralized, deterministic rules for the most common D&amp;D 5e calculations.
/// Kept in the domain layer so every consumer shares the same behaviour.
/// </summary>
public static class Rules
{
    /// <summary>
    /// Computes the ability score modifier for a score:
    /// <c>(score - 10) / 2</c> rounded toward negative infinity.
    /// </summary>
    public static int Modifier(int score) => (int)Math.Floor((score - 10) / 2d);

    /// <summary>
    /// Computes the proficiency bonus for a given character level
    /// (2 + 1 for every 4 full levels above 1).
    /// </summary>
    public static int ProficiencyBonus(int level) => 2 + Math.Max(0, (level - 1) / 4);

    /// <summary>
    /// Passive perception defaults to 10 + the wisdom modifier.
    /// </summary>
    public static int PassivePerception(int wisdomModifier) => 10 + wisdomModifier;

    /// <summary>
    /// Clamps a value so it never drops below zero.
    /// </summary>
    public static int ClampToZero(int value) => value < 0 ? 0 : value;

    /// <summary>
    /// Clamps a value between an inclusive lower and upper bound.
    /// </summary>
    public static int Clamp(int value, int min, int max)
    {
        if (max < min)
        {
            return min;
        }

        return value < min ? min : value > max ? max : value;
    }
}
