namespace DndCompanion.Core.SkillChecks;

/// <summary>
/// Exception thrown by the skill check service when a check cannot be
/// created or recorded.
/// </summary>
public sealed class SkillCheckException : Exception
{
    public SkillCheckException(string message)
        : base(message)
    {
    }

    public SkillCheckException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}