namespace DndCompanion.Core.Story;

/// <summary>
/// Application service that manages the scenes of a campaign's chapters.
/// Every method operates on a single campaign identified by its folder path.
/// </summary>
public interface ISceneService
{
    /// <summary>
    /// Creates a new scene appended at the end of the given chapter.
    /// </summary>
    /// <exception cref="StoryException">When the chapter does not exist or cannot be accessed.</exception>
    Task<SceneInfo> CreateSceneAsync(
        string campaignFolderPath,
        Guid chapterId,
        string title,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the title and description of an existing scene.</summary>
    /// <exception cref="StoryException">When the scene does not exist or cannot be updated.</exception>
    Task<SceneInfo> UpdateSceneAsync(
        string campaignFolderPath,
        Guid sceneId,
        string title,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a scene.</summary>
    /// <exception cref="StoryException">When the scene does not exist or cannot be deleted.</exception>
    Task DeleteSceneAsync(
        string campaignFolderPath,
        Guid sceneId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a scene to another position, optionally into a different chapter,
    /// renumbering the scenes of the affected chapters.
    /// </summary>
    /// <exception cref="StoryException">When the scene or target chapter does not exist.</exception>
    Task<SceneInfo> MoveSceneAsync(
        string campaignFolderPath,
        Guid sceneId,
        Guid targetChapterId,
        int newOrder,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a scene as completed.</summary>
    /// <exception cref="StoryException">When the scene does not exist.</exception>
    Task<SceneInfo> CompleteSceneAsync(
        string campaignFolderPath,
        Guid sceneId,
        CancellationToken cancellationToken = default);

    /// <summary>Sets the given scene as the current scene of the campaign.</summary>
    /// <exception cref="StoryException">When the scene does not exist.</exception>
    Task<SceneInfo> SetCurrentSceneAsync(
        string campaignFolderPath,
        Guid sceneId,
        CancellationToken cancellationToken = default);
}