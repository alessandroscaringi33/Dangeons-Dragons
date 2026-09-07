namespace DndCompanion.Core.Campaigns;

/// <summary>
/// Readiness state of a campaign as surfaced by the campaign management UI.
/// </summary>
public enum CampaignStatus
{
    /// <summary>The campaign has a database file and can be opened.</summary>
    Ready,

    /// <summary>The campaign folder exists but has no database yet.</summary>
    NeedsInitialization,

    /// <summary>The campaign folder could not be read.</summary>
    Error
}