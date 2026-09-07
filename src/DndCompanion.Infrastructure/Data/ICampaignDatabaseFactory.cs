namespace DndCompanion.Infrastructure.Data;

/// <summary>
/// Creates <see cref="CampaignDbContext"/> instances bound to a specific
/// campaign folder. Each campaign owns its own SQLite database file
/// (<c>campaign.db</c>) inside its folder.
/// </summary>
public interface ICampaignDatabaseFactory
{
    /// <summary>Name of the database file inside a campaign folder.</summary>
    string DatabaseFileName { get; }

    /// <summary>Absolute path of the database file for the given campaign folder.</summary>
    string GetDatabaseFilePath(string campaignFolderPath);

    /// <summary>Creates a context for the given campaign folder.</summary>
    CampaignDbContext CreateContext(string campaignFolderPath);
}
