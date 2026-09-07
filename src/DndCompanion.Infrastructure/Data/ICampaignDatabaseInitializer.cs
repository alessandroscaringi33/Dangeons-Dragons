namespace DndCompanion.Infrastructure.Data;

/// <summary>
/// Ensures the database of a campaign exists and is up to date with the
/// latest schema migrations.
/// </summary>
public interface ICampaignDatabaseInitializer
{
    /// <summary>
    /// Creates the campaign folder and database file when needed and applies
    /// any pending migrations.
    /// </summary>
    /// <param name="campaignFolderPath">Folder that owns the campaign database.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(string campaignFolderPath, CancellationToken cancellationToken = default);
}
