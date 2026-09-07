using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Quests;

namespace DndCompanion.ViewModels;

/// <summary>
/// Presentation model of a single quest for the world list. Active quests are
/// visually highlighted.
/// </summary>
public sealed class QuestItemViewModel : ViewModelBase
{
    private static readonly Color ActiveColor = Color.FromArgb("#B08D57");
    private static readonly Color NotStartedColor = Color.FromArgb("#9E9E9E");
    private static readonly Color CompletedColor = Color.FromArgb("#4CAF50");
    private static readonly Color FailedColor = Color.FromArgb("#F44336");
    private static readonly Color AbandonedColor = Color.FromArgb("#757575");

    public QuestItemViewModel(QuestInfo quest)
    {
        Quest = quest;
    }

    public QuestInfo Quest { get; }

    public Guid Id => Quest.Id;

    public string Title => Quest.Title;

    public string Description => Quest.Description;

    public string StatusText => Quest.Status switch
    {
        QuestStatus.NotStarted => "Non iniziata",
        QuestStatus.Active => "Attiva",
        QuestStatus.Completed => "Completata",
        QuestStatus.Failed => "Fallita",
        QuestStatus.Abandoned => "Abbandonata",
        _ => Quest.Status.ToString()
    };

    public Color StatusColor => Quest.Status switch
    {
        QuestStatus.Active => ActiveColor,
        QuestStatus.Completed => CompletedColor,
        QuestStatus.Failed => FailedColor,
        QuestStatus.Abandoned => AbandonedColor,
        _ => NotStartedColor
    };

    public bool IsActive => Quest.Status == QuestStatus.Active;

    private static readonly Color ActiveBackgroundColor = Color.FromArgb("#2A2218");
    private static readonly Color ActiveStrokeColor = Color.FromArgb("#B08D57");
    private static readonly Color NormalBackgroundColor = Color.FromArgb("#1C1C1C");
    private static readonly Color NormalStrokeColor = Color.FromArgb("#2E2E2E");

    public Color BackgroundColor => IsActive ? ActiveBackgroundColor : NormalBackgroundColor;

    public Color StrokeColor => IsActive ? ActiveStrokeColor : NormalStrokeColor;

    public double StrokeThickness => IsActive ? 2 : 1;

    public string LinkText
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Quest.SceneTitle))
            {
                parts.Add($"Scena: {Quest.SceneTitle}");
            }

            if (!string.IsNullOrWhiteSpace(Quest.LocationName))
            {
                parts.Add($"Luogo: {Quest.LocationName}");
            }

            if (!string.IsNullOrWhiteSpace(Quest.NpcName))
            {
                parts.Add($"NPC: {Quest.NpcName}");
            }

            return parts.Count == 0 ? "Nessun collegamento" : string.Join("  ·  ", parts);
        }
    }

    public bool HasNotes => !string.IsNullOrEmpty(Quest.Notes);
}