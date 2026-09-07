namespace DndCompanion.Core.Campaigns;

/// <summary>
/// Supplies the location of the <c>Campagne</c> root folder. The location is
/// configurable so it can be changed from the settings screen later.
/// </summary>
public interface ICampaignsPathProvider
{
    string CampaignsFolderPath { get; }
}