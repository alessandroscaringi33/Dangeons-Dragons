namespace DndCompanion.Core.Campaigns;

/// <summary>
/// Lightweight, UI-facing description of a campaign discovered on disk.
/// </summary>
public sealed class CampaignInfo
{
    /// <summary>Display name (the campaign folder name).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Absolute path of the campaign folder.</summary>
    public string FolderPath { get; init; } = string.Empty;

    /// <summary>Absolute path of the campaign database file, if any.</summary>
    public string DatabasePath { get; init; } = string.Empty;

    /// <summary>Names of the PDF files found directly inside the folder.</summary>
    public IReadOnlyList<string> PdfFiles { get; init; } = Array.Empty<string>();

    /// <summary>Last write time of the campaign folder.</summary>
    public DateTime LastModified { get; init; }

    /// <summary>True when a <c>campaign.db</c> file exists in the folder.</summary>
    public bool HasDatabase { get; init; }

    public CampaignStatus Status { get; init; }

    public bool HasPdf => PdfFiles.Count > 0;
}