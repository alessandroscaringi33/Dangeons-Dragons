using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Quests;

/// <summary>
/// Editable profile of a quest used to create or update it.
/// </summary>
public sealed class QuestDraft
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public QuestStatus Status { get; set; } = QuestStatus.NotStarted;

    public string Notes { get; set; } = string.Empty;

    public Guid? ChapterId { get; set; }

    public Guid? SceneId { get; set; }

    public Guid? LocationId { get; set; }

    public Guid? NpcId { get; set; }
}