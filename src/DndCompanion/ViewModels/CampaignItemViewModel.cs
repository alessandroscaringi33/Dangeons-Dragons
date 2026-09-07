using DndCompanion.Core.Campaigns;
using DndCompanion.Core.Mvvm;

namespace DndCompanion.ViewModels;

/// <summary>
/// Read-only presentation model of a single campaign for the campaigns list.
/// </summary>
public sealed class CampaignItemViewModel : ViewModelBase
{
    private static readonly Color ReadyColor = Color.FromArgb("#4CAF50");
    private static readonly Color NeedsInitializationColor = Color.FromArgb("#FF9800");
    private static readonly Color ErrorColor = Color.FromArgb("#F44336");
    private static readonly Color UnknownColor = Color.FromArgb("#9E9E9E");

    public CampaignItemViewModel(CampaignInfo campaign)
    {
        Campaign = campaign;
    }

    public CampaignInfo Campaign { get; }

    public string Name => Campaign.Name;

    public string PdfText => Campaign.HasPdf
        ? string.Join(", ", Campaign.PdfFiles)
        : "Nessun PDF";

    public string LastModifiedText => Campaign.LastModified.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public string StatusText => Campaign.Status switch
    {
        CampaignStatus.Ready => "Pronta",
        CampaignStatus.NeedsInitialization => "Da inizializzare",
        CampaignStatus.Error => "Errore",
        _ => "Sconosciuto"
    };

    public Color StatusColor => Campaign.Status switch
    {
        CampaignStatus.Ready => ReadyColor,
        CampaignStatus.NeedsInitialization => NeedsInitializationColor,
        CampaignStatus.Error => ErrorColor,
        _ => UnknownColor
    };
}