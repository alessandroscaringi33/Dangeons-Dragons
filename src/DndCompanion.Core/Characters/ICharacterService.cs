using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Characters;

/// <summary>
/// Application service that manages the player characters of a campaign.
/// Every method operates on a single campaign identified by its folder path.
/// Derived values are always computed and never persisted.
/// </summary>
public interface ICharacterService
{
    /// <summary>Lists the characters of a campaign.</summary>
    Task<IReadOnlyList<CharacterInfo>> ListCharactersAsync(
        string campaignFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a single character by id.</summary>
    /// <exception cref="CharacterException">When the character does not exist.</exception>
    Task<CharacterInfo> GetCharacterAsync(
        string campaignFolderPath,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a new character in the campaign.</summary>
    /// <exception cref="CharacterException">When creation fails.</exception>
    Task<CharacterInfo> CreateCharacterAsync(
        string campaignFolderPath,
        CharacterDraft draft,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the profile fields of an existing character.</summary>
    /// <exception cref="CharacterException">When the character does not exist.</exception>
    Task<CharacterInfo> UpdateCharacterAsync(
        string campaignFolderPath,
        Guid characterId,
        CharacterDraft draft,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a character and its inventory.</summary>
    /// <exception cref="CharacterException">When the character does not exist.</exception>
    Task DeleteCharacterAsync(
        string campaignFolderPath,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies damage to a character (temporary HP first, then real HP).
    /// </summary>
    Task<CharacterInfo> ApplyDamageAsync(
        string campaignFolderPath,
        Guid characterId,
        int amount,
        CancellationToken cancellationToken = default);

    /// <summary>Restores HP up to the maximum.</summary>
    Task<CharacterInfo> HealAsync(
        string campaignFolderPath,
        Guid characterId,
        int amount,
        CancellationToken cancellationToken = default);

    /// <summary>Sets the current HP to an exact value (clamped to 0..max).</summary>
    Task<CharacterInfo> SetCurrentHpAsync(
        string campaignFolderPath,
        Guid characterId,
        int hp,
        CancellationToken cancellationToken = default);

    /// <summary>Sets the temporary HP to an exact value (never negative).</summary>
    Task<CharacterInfo> SetTemporaryHpAsync(
        string campaignFolderPath,
        Guid characterId,
        int hp,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a condition to the character.</summary>
    Task<CharacterInfo> AddConditionAsync(
        string campaignFolderPath,
        Guid characterId,
        CharacterCondition condition,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a condition from the character.</summary>
    Task<CharacterInfo> RemoveConditionAsync(
        string campaignFolderPath,
        Guid characterId,
        CharacterCondition condition,
        CancellationToken cancellationToken = default);

    /// <summary>Adds an item to the character's inventory.</summary>
    Task<CharacterInfo> AddInventoryItemAsync(
        string campaignFolderPath,
        Guid characterId,
        string name,
        int quantity,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes an item from the character's inventory.</summary>
    Task<CharacterInfo> RemoveInventoryItemAsync(
        string campaignFolderPath,
        Guid characterId,
        Guid itemId,
        CancellationToken cancellationToken = default);
}