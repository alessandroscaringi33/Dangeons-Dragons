namespace DndCompanion.Core.Locations;

/// <summary>
/// Application service that manages the locations of a campaign.
/// Every method operates on a single campaign identified by its folder path.
/// </summary>
public interface ILocationService
{
    /// <summary>Lists the locations of a campaign, optionally filtered by a
    /// case-insensitive search term over name and description.</summary>
    Task<IReadOnlyList<LocationInfo>> ListLocationsAsync(
        string campaignFolderPath,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a single location by id.</summary>
    /// <exception cref="LocationException">When the location does not exist.</exception>
    Task<LocationInfo> GetLocationAsync(
        string campaignFolderPath,
        Guid locationId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a new location in the campaign.</summary>
    /// <exception cref="LocationException">When creation fails.</exception>
    Task<LocationInfo> CreateLocationAsync(
        string campaignFolderPath,
        string name,
        string description,
        string notes = "",
        CancellationToken cancellationToken = default);

    /// <summary>Updates the profile of an existing location.</summary>
    /// <exception cref="LocationException">When the location does not exist.</exception>
    Task<LocationInfo> UpdateLocationAsync(
        string campaignFolderPath,
        Guid locationId,
        string name,
        string description,
        string notes = "",
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a location. NPCs linked to it are unlinked.</summary>
    /// <exception cref="LocationException">When the location does not exist.</exception>
    Task DeleteLocationAsync(
        string campaignFolderPath,
        Guid locationId,
        CancellationToken cancellationToken = default);
}