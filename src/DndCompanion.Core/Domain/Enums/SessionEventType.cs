namespace DndCompanion.Core.Domain.Enums;

/// <summary>Categorizes the entries recorded in the session timeline.</summary>
public enum SessionEventType
{
    SessionStarted,
    SessionEnded,
    SceneEntered,
    NpcMet,
    NpcDefeated,
    CombatStarted,
    CombatEnded,
    DiceRolled,
    SkillCheck,
    QuestStarted,
    QuestProgressed,
    QuestCompleted,
    QuestFailed,
    LocationVisited,
    NoteAdded,
    Other
}
