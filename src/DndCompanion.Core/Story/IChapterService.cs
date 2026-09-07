namespace DndCompanion.Core.Story;

/// <summary>
/// Application service that manages the chapters of a campaign's story.
/// Every method operates on a single campaign identified by its folder path.
/// </summary>
public interface IChapterService
{
    /// <summary>
    /// Lists the chapters of a campaign ordered by position, each with its
    /// ordered scenes and the current scene of the campaign.
    /// </summary>
    Task<IReadOnlyList<ChapterInfo>> ListChaptersAsync(
        string campaignFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a new chapter appended at the end of the story.</summary>
    /// <exception cref="StoryException">When the campaign or its database cannot be accessed.</exception>
    Task<ChapterInfo> CreateChapterAsync(
        string campaignFolderPath,
        string title,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the title and description of an existing chapter.</summary>
    /// <exception cref="StoryException">When the chapter does not exist or cannot be updated.</exception>
    Task<ChapterInfo> UpdateChapterAsync(
        string campaignFolderPath,
        Guid chapterId,
        string title,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a chapter and its scenes.</summary>
    /// <exception cref="StoryException">When the chapter does not exist or cannot be deleted.</exception>
    Task DeleteChapterAsync(
        string campaignFolderPath,
        Guid chapterId,
        CancellationToken cancellationToken = default);

    /// <summary>Moves a chapter to a new 1-based position, renumbering the others.</summary>
    /// <exception cref="StoryException">When the chapter does not exist or cannot be moved.</exception>
    Task<ChapterInfo> MoveChapterAsync(
        string campaignFolderPath,
        Guid chapterId,
        int newOrder,
        CancellationToken cancellationToken = default);
}