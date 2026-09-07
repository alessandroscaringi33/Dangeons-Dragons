using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Campaigns;
using DndCompanion.Core.Documents;
using DndCompanion.Core.Mvvm;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the "Storia / Documento" screen: lists the PDFs of the
/// opened campaign and shows the result of importing (text extraction) the
/// selected document.
/// </summary>
public sealed partial class DocumentViewModel : ViewModelBase
{
    private static readonly Color ReadyColor = Color.FromArgb("#4CAF50");
    private static readonly Color NoTextColor = Color.FromArgb("#FF9800");
    private static readonly Color ErrorColor = Color.FromArgb("#F44336");
    private static readonly Color NeutralColor = Color.FromArgb("#9E9E9E");

    private readonly ICampaignService _campaignService;
    private readonly IPdfReader _pdfReader;
    private readonly ILogger<DocumentViewModel> _logger;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PdfItemViewModel> pdfDocuments = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasPdf;

    [ObservableProperty]
    private bool hasDocument;

    [ObservableProperty]
    private string selectedPdfName = string.Empty;

    [ObservableProperty]
    private string pageCountText = string.Empty;

    [ObservableProperty]
    private string importStatusText = string.Empty;

    [ObservableProperty]
    private Color importStatusColor = NeutralColor;

    [ObservableProperty]
    private string documentStatusDetail = string.Empty;

    [ObservableProperty]
    private string? extractedText;

    public DocumentViewModel(
        ICampaignService campaignService,
        IPdfReader pdfReader,
        ILogger<DocumentViewModel> logger)
    {
        _campaignService = campaignService;
        _pdfReader = pdfReader;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

    public bool HasDocumentStatusDetail => !string.IsNullOrEmpty(DocumentStatusDetail);

    partial void OnDocumentStatusDetailChanged(string value) => OnPropertyChanged(nameof(HasDocumentStatusDetail));

    public bool HasExtractedText => !string.IsNullOrEmpty(ExtractedText);

    partial void OnExtractedTextChanged(string? value) => OnPropertyChanged(nameof(HasExtractedText));

    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var pdfNames = await _campaignService.FindPdfFilesAsync(FolderPath);
            PdfDocuments.Clear();
            foreach (var name in pdfNames)
            {
                PdfDocuments.Add(new PdfItemViewModel(FolderPath, name));
            }

            HasPdf = PdfDocuments.Count > 0;

            if (PdfDocuments.Count == 0)
            {
                HasDocument = false;
                StatusMessage = "Nessun documento PDF trovato in questa campagna.";
            }
            else if (PdfDocuments.Count == 1)
            {
                await ImportAsync(PdfDocuments[0]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load documents for campaign in {Folder}", FolderPath);
            HasDocument = false;
            StatusMessage = "Impossibile caricare i documenti della campagna.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ImportAsync(PdfItemViewModel item)
    {
        if (item is null)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var info = await _pdfReader.ExtractAsync(item.FilePath);

            SelectedPdfName = info.FileName;
            PageCountText = info.PageCount > 0
                ? $"{(info.PageCount == 1 ? "1 pagina" : $"{info.PageCount} pagine")}"
                : "Pagine non disponibili";

            switch (info.Status)
            {
                case PdfImportStatus.Imported:
                    ImportStatusText = "Testo estratto";
                    ImportStatusColor = ReadyColor;
                    ExtractedText = info.ExtractedText;
                    DocumentStatusDetail = string.Empty;
                    break;

                case PdfImportStatus.NoText:
                    ImportStatusText = "Nessun testo estraibile";
                    ImportStatusColor = NoTextColor;
                    ExtractedText = null;
                    DocumentStatusDetail = info.Message ?? "Il PDF non contiene testo estraibile.";
                    break;

                default:
                    ImportStatusText = "Importazione non riuscita";
                    ImportStatusColor = ErrorColor;
                    ExtractedText = null;
                    DocumentStatusDetail = info.Message ?? "Non è stato possibile leggere il PDF.";
                    break;
            }

            HasDocument = true;
            _logger.LogInformation(
                "Imported PDF '{File}' for campaign '{Campaign}' with status {Status}",
                info.FileName,
                CampaignName,
                info.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import PDF '{Path}'", item.FilePath);
            HasDocument = false;
            StatusMessage = "Impossibile leggere il documento PDF.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}