using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Npcs;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the DM's NPC list screen: search, quick HP controls and
/// alive/dead toggle. Dialog prompts are handled by the page code-behind.
/// </summary>
public sealed partial class NpcsViewModel : ViewModelBase
{
    private readonly INpcService _npcService;
    private readonly ILogger<NpcsViewModel> _logger;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<NpcItemViewModel> npcs = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasNpcs;

    public NpcsViewModel(
        INpcService npcService,
        ILogger<NpcsViewModel> logger)
    {
        _npcService = npcService;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool HasNoNpcs => !HasNpcs;

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

    partial void OnHasNpcsChanged(bool value) => OnPropertyChanged(nameof(HasNoNpcs));

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
            var npcs = await _npcService.ListNpcsAsync(FolderPath, SearchText);
            Rebuild(npcs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load NPCs of campaign in {Folder}", FolderPath);
            StatusMessage = "Impossibile caricare gli NPC.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SearchAsync(string searchText)
    {
        SearchText = searchText ?? string.Empty;
        await LoadAsync();
    }

    public async Task CreateNpcAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.CreateNpcAsync(FolderPath, new NpcDraft { Name = name, MaxHp = 10, CurrentHp = 10 }),
            $"NPC '{name.Trim()}' creato.",
            "Impossibile creare l'NPC.");
    }

    public async Task DeleteNpcAsync(NpcItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.DeleteNpcAsync(FolderPath, item.Id),
            $"NPC '{item.Name}' eliminato.",
            "Impossibile eliminare l'NPC.");
    }

    public async Task ApplyDamageAsync(NpcItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.ApplyDamageAsync(FolderPath, item.Id, amount),
            null,
            "Impossibile applicare il danno.");
    }

    public async Task HealAsync(NpcItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.HealAsync(FolderPath, item.Id, amount),
            null,
            "Impossibile curare l'NPC.");
    }

    public async Task SetCurrentHpAsync(NpcItemViewModel? item, int hp)
    {
        if (item is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.SetCurrentHpAsync(FolderPath, item.Id, hp),
            $"HP di '{item.Name}' impostati a {hp}.",
            "Impossibile impostare gli HP.");
    }

    public async Task ToggleAliveAsync(NpcItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.SetAliveAsync(FolderPath, item.Id, !item.Npc.IsAlive),
            null,
            "Impossibile aggiornare lo stato.");
    }

    private void Rebuild(IReadOnlyList<NpcInfo> npcs)
    {
        Npcs.Clear();
        foreach (var npc in npcs)
        {
            Npcs.Add(new NpcItemViewModel(npc));
        }

        HasNpcs = Npcs.Count > 0;
    }

    private async Task ExecuteAsync(Func<Task> action, string? successMessage, string errorMessage)
    {
        try
        {
            IsLoading = true;
            await action();

            if (successMessage is not null)
            {
                StatusMessage = successMessage;
            }

            await ReloadAsync();
        }
        catch (NpcException ex)
        {
            _logger.LogWarning(ex, "{Message}", ex.Message);
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", errorMessage);
            StatusMessage = errorMessage;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ReloadAsync()
    {
        try
        {
            var npcs = await _npcService.ListNpcsAsync(FolderPath, SearchText);
            Rebuild(npcs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload NPCs in {Folder}", FolderPath);
        }
    }
}