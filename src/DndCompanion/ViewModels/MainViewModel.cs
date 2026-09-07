using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Mvvm;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public string Title { get; } = "D&D Companion";

    public MainViewModel(ILogger<MainViewModel> logger)
    {
        _logger = logger;
        _logger.LogInformation("MainViewModel initialized");
        StatusMessage = "Pronto per la sessione";
    }
}