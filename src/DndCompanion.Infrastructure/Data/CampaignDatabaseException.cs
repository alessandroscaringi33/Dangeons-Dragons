namespace DndCompanion.Infrastructure.Data;

/// <summary>
/// Thrown when a campaign database cannot be created, opened or migrated.
/// Carries an inner exception with the technical detail for logging.
/// </summary>
public sealed class CampaignDatabaseException : Exception
{
    public CampaignDatabaseException(string message)
        : base(message)
    {
    }

    public CampaignDatabaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
