namespace DndCompanion.Core.SkillChecks;

/// <summary>
/// UI-facing description of a recorded skill check.
/// </summary>
public sealed class SkillCheckInfo
{
    public Guid Id { get; init; }

    public Guid SessionId { get; init; }

    public Guid? CharacterId { get; init; }

    /// <summary>Name of the character, when linked.</summary>
    public string? CharacterName { get; init; }

    public string Skill { get; init; } = string.Empty;

    public int Roll { get; init; }

    public int Modifier { get; init; }

    public int DifficultyClass { get; init; }

    public bool IsPhysicalRoll { get; init; }

    public DateTime Timestamp { get; init; }

    public int Total { get; init; }

    public bool IsSuccess { get; init; }

    public string OutcomeText => IsSuccess ? "SUCCESSO" : "FALLIMENTO";
}