using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Quests;

/// <summary>
/// Application service that manages the quests of a campaign. Every method
/// operates on a single campaign identified by its folder path.
/// </summary>
public interface IQuestService
{
    /// <summary>Lists the quests of a campaign, optionally filtered by a
    /// case-insensitive search term over title and description.</summary>
    Task<IReadOnlyList<QuestInfo>> ListQuestsAsync(
        string campaignFolderPath,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a single quest by id.</summary>
    /// <exception cref="QuestException">When the quest does not exist.</exception>
    Task<QuestInfo> GetQuestAsync(
        string campaignFolderPath,
        Guid questId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a new quest in the campaign.</summary>
    /// <exception cref="QuestException">When creation fails.</exception>
    Task<QuestInfo> CreateQuestAsync(
        string campaignFolderPath,
        QuestDraft draft,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the profile of an existing quest.</summary>
    /// <exception cref="QuestException">When the quest does not exist.</exception>
    Task<QuestInfo> UpdateQuestAsync(
        string campaignFolderPath,
        Guid questId,
        QuestDraft draft,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a quest.</summary>
    /// <exception cref="QuestException">When the quest does not exist.</exception>
    Task DeleteQuestAsync(
        string campaignFolderPath,
        Guid questId,
        CancellationToken cancellationToken = default);

    /// <summary>Changes the status of a quest.</summary>
    /// <exception cref="QuestException">When the quest does not exist.</exception>
    Task<QuestInfo> SetStatusAsync(
        string campaignFolderPath,
        Guid questId,
        QuestStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>Links a quest to a chapter (or clears the link when null).</summary>
    Task<QuestInfo> LinkToChapterAsync(
        string campaignFolderPath,
        Guid questId,
        Guid? chapterId,
        CancellationToken cancellationToken = default);

    /// <summary>Links a quest to a scene (or clears the link when null).</summary>
    Task<QuestInfo> LinkToSceneAsync(
        string campaignFolderPath,
        Guid questId,
        Guid? sceneId,
        CancellationToken cancellationToken = default);

    /// <summary>Links a quest to a location (or clears the link when null).</summary>
    Task<QuestInfo> LinkToLocationAsync(
        string campaignFolderPath,
        Guid questId,
        Guid? locationId,
        CancellationToken cancellationToken = default);

    /// <summary>Links a quest to an NPC (or clears the link when null).</summary>
    Task<QuestInfo> LinkToNpcAsync(
        string campaignFolderPath,
        Guid questId,
        Guid? npcId,
        CancellationToken cancellationToken = default);
}