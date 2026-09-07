using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.Data;

public sealed class CampaignDatabaseInitializer : ICampaignDatabaseInitializer
{
    private readonly ICampaignDatabaseFactory _factory;
    private readonly ILogger<CampaignDatabaseInitializer> _logger;

    public CampaignDatabaseInitializer(
        ICampaignDatabaseFactory factory,
        ILogger<CampaignDatabaseInitializer> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task InitializeAsync(string campaignFolderPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignFolderPath);

        var databasePath = _factory.GetDatabaseFilePath(campaignFolderPath);

        try
        {
            Directory.CreateDirectory(campaignFolderPath);

            await using var context = _factory.CreateContext(campaignFolderPath);
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Campaign database is ready at {DatabasePath}", databasePath);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize campaign database at {DatabasePath}", databasePath);
            throw new CampaignDatabaseException(
                $"The campaign database at '{databasePath}' could not be initialized.", ex);
        }
    }
}
