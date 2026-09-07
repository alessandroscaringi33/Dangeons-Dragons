using DndCompanion.Core.Mvvm;

namespace DndCompanion.ViewModels;

/// <summary>Presentation model of a PDF file inside a campaign.</summary>
public sealed class PdfItemViewModel : ViewModelBase
{
    public PdfItemViewModel(string folderPath, string fileName)
    {
        FolderPath = folderPath;
        FileName = fileName;
    }

    public string FolderPath { get; }

    public string FileName { get; }

    public string FilePath => Path.Combine(FolderPath, FileName);
}