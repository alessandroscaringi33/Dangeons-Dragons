namespace DndCompanion.Core.Campaigns;

/// <summary>
/// Application service that manages campaign folders on disk and their
/// databases. Never modifies or deletes the PDF files found in a campaign.
/// </summary>
public interface ICampaignService
{
    /// <summary>Absolute path of the configured <c>Campagne</c> folder.</summary>
    string CampaignsFolderPath { get; }

    /// <summary>Lists the campaigns found inside the <c>Campagne</c> folder.</summary>
    Task<IReadOnlyList<CampaignInfo>> ListCampaignsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new campaign: folder, database and initial campaign record.
    /// </summary>
    /// <exception cref="CampaignException">When creation fails or a campaign with the same name already exists.</exception>
    Task<CampaignInfo> CreateCampaignAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an existing campaign, creating or migrating its database when
    /// needed and verifying it is usable.
    /// </summary>
    /// <exception cref="CampaignException">When the campaign does not exist or cannot be opened.</exception>
    Task<CampaignInfo> OpenCampaignAsync(string campaignFolderName, CancellationToken cancellationToken = default);

    /// <summary>Finds the PDF files directly inside a campaign folder.</summary>
    Task<IReadOnlyList<string>> FindPdfFilesAsync(string campaignFolderPath, CancellationToken cancellationToken = default);
}