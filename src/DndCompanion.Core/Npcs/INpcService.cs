namespace DndCompanion.Core.Npcs;

/// <summary>
/// Application service that manages the non-player characters of a campaign.
/// Every method operates on a single campaign identified by its folder path.
/// </summary>
public interface INpcService
{
    /// <summary>Lists the NPCs of a campaign, optionally filtered by a
    /// case-insensitive search term over name, role and description.</summary>
    Task<IReadOnlyList<NpcInfo>> ListNpcsAsync(
        string campaignFolderPath,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a single NPC by id.</summary>
    /// <exception cref="NpcException">When the NPC does not exist.</exception>
    Task<NpcInfo> GetNpcAsync(
        string campaignFolderPath,
        Guid npcId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a new NPC in the campaign.</summary>
    /// <exception cref="NpcException">When creation fails.</exception>
    Task<NpcInfo> CreateNpcAsync(
        string campaignFolderPath,
        NpcDraft draft,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the profile of an existing NPC.</summary>
    /// <exception cref="NpcException">When the NPC does not exist.</exception>
    Task<NpcInfo> UpdateNpcAsync(
        string campaignFolderPath,
        Guid npcId,
        NpcDraft draft,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes an NPC.</summary>
    /// <exception cref="NpcException">When the NPC does not exist.</exception>
    Task DeleteNpcAsync(
        string campaignFolderPath,
        Guid npcId,
        CancellationToken cancellationToken = default);

    /// <summary>Applies damage to an NPC, marking it dead at zero HP.</summary>
    Task<NpcInfo> ApplyDamageAsync(
        string campaignFolderPath,
        Guid npcId,
        int amount,
        CancellationToken cancellationToken = default);

    /// <summary>Restores HP up to the maximum, reviving the NPC.</summary>
    Task<NpcInfo> HealAsync(
        string campaignFolderPath,
        Guid npcId,
        int amount,
        CancellationToken cancellationToken = default);

    /// <summary>Sets the current HP to an exact value (clamped to 0..max).</summary>
    Task<NpcInfo> SetCurrentHpAsync(
        string campaignFolderPath,
        Guid npcId,
        int hp,
        CancellationToken cancellationToken = default);

    /// <summary>Explicitly sets the alive/dead state of an NPC.</summary>
    Task<NpcInfo> SetAliveAsync(
        string campaignFolderPath,
        Guid npcId,
        bool isAlive,
        CancellationToken cancellationToken = default);

    /// <summary>Links an NPC to a scene (or clears the link when null).</summary>
    Task<NpcInfo> LinkToSceneAsync(
        string campaignFolderPath,
        Guid npcId,
        Guid? sceneId,
        CancellationToken cancellationToken = default);

    /// <summary>Links an NPC to a location (or clears the link when null).</summary>
    Task<NpcInfo> LinkToLocationAsync(
        string campaignFolderPath,
        Guid npcId,
        Guid? locationId,
        CancellationToken cancellationToken = default);
}