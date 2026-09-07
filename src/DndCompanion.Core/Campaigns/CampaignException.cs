namespace DndCompanion.Core.Campaigns;

/// <summary>
/// Thrown when a campaign cannot be created, opened or listed for a reason
/// the caller should surface to the user.
/// </summary>
public sealed class CampaignException : Exception
{
    public CampaignException(string message)
        : base(message)
    {
    }

    public CampaignException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}