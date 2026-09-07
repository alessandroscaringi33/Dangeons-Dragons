using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Infrastructure.Data;

public sealed class CampaignDatabaseFactory : ICampaignDatabaseFactory
{
    public const string DefaultDatabaseFileName = "campaign.db";

    public string DatabaseFileName => DefaultDatabaseFileName;

    public string GetDatabaseFilePath(string campaignFolderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignFolderPath);
        return Path.Combine(campaignFolderPath, DefaultDatabaseFileName);
    }

    public CampaignDbContext CreateContext(string campaignFolderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignFolderPath);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = GetDatabaseFilePath(campaignFolderPath)
        }.ToString();

        var options = new DbContextOptionsBuilder<CampaignDbContext>()
            .UseSqlite(connectionString, sqlite =>
                sqlite.MigrationsAssembly(typeof(CampaignDbContext).Assembly.FullName))
            .Options;

        return new CampaignDbContext(options);
    }
}
