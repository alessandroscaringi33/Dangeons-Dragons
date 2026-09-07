using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Characters;
using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Npcs;
using DndCompanion.Core.Story;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the in-session quick view: shows the current scene and gives
/// the DM fast access to NPC and character HP plus alive/dead state.
/// </summary>
public sealed partial class SessionQuickViewViewModel : ViewModelBase
{
    private readonly INpcService _npcService;
    private readonly IChapterService _chapterService;
    private readonly ICharacterService _characterService;
    private readonly ILogger<SessionQuickViewViewModel> _logger;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private string currentSceneText = "Nessuna scena corrente";

    [ObservableProperty]
    private ObservableCollection<NpcItemViewModel> npcs = new();

    [ObservableProperty]
    private ObservableCollection<CharacterItemViewModel> characters = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasNpcs;

    [ObservableProperty]
    private bool hasCharacters;

    public SessionQuickViewViewModel(
        INpcService npcService,
        IChapterService chapterService,
        ICharacterService characterService,
        ILogger<SessionQuickViewViewModel> logger)
    {
        _npcService = npcService;
        _chapterService = chapterService;
        _characterService = characterService;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool HasNoNpcs => !HasNpcs;

    public bool HasNoCharacters => !HasCharacters;

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));
    partial void OnHasNpcsChanged(bool value) => OnPropertyChanged(nameof(HasNoNpcs));
    partial void OnHasCharactersChanged(bool value) => OnPropertyChanged(nameof(HasNoCharacters));

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
            await LoadCurrentSceneAsync();
            await LoadNpcsAsync();
            await LoadCharactersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session quick view for {Folder}", FolderPath);
            StatusMessage = "Impossibile caricare la sessione.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ApplyNpcDamageAsync(NpcItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteNpcAsync(
            () => _npcService.ApplyDamageAsync(FolderPath, item.Id, amount));
    }

    public async Task HealNpcAsync(NpcItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteNpcAsync(
            () => _npcService.HealAsync(FolderPath, item.Id, amount));
    }

    public async Task ToggleNpcAliveAsync(NpcItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await ExecuteNpcAsync(
            () => _npcService.SetAliveAsync(FolderPath, item.Id, !item.Npc.IsAlive));
    }

    public async Task ApplyCharacterDamageAsync(CharacterItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteCharacterAsync(
            () => _characterService.ApplyDamageAsync(FolderPath, item.Id, amount));
    }

    public async Task HealCharacterAsync(CharacterItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteCharacterAsync(
            () => _characterService.HealAsync(FolderPath, item.Id, amount));
    }

    private async Task LoadCurrentSceneAsync()
    {
        var chapters = await _chapterService.ListChaptersAsync(FolderPath);
        var currentSceneId = chapters.FirstOrDefault(c => c.CurrentSceneId is not null)?.CurrentSceneId;

        if (currentSceneId is Guid sceneId)
        {
            var match = chapters
                .SelectMany(c => c.Scenes.Select(s => new { Chapter = c.Title, Scene = s }))
                .FirstOrDefault(x => x.Scene.Id == sceneId);

            if (match is not null)
            {
                CurrentSceneText = $"Scena: {match.Scene.Title}  ·  {match.Chapter}";
                return;
            }
        }

        CurrentSceneText = "Nessuna scena corrente";
    }

    private async Task LoadNpcsAsync()
    {
        var npcs = await _npcService.ListNpcsAsync(FolderPath);
        Npcs.Clear();
        foreach (var npc in npcs)
        {
            Npcs.Add(new NpcItemViewModel(npc));
        }

        HasNpcs = Npcs.Count > 0;
    }

    private async Task LoadCharactersAsync()
    {
        var characters = await _characterService.ListCharactersAsync(FolderPath);
        Characters.Clear();
        foreach (var character in characters)
        {
            Characters.Add(new CharacterItemViewModel(character));
        }

        HasCharacters = Characters.Count > 0;
    }

    private async Task ExecuteNpcAsync(Func<Task> action)
    {
        try
        {
            IsLoading = true;
            await action();
            await LoadNpcsAsync();
        }
        catch (NpcException ex)
        {
            _logger.LogWarning(ex, "{Message}", ex.Message);
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update NPC");
            StatusMessage = "Impossibile aggiornare l'NPC.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteCharacterAsync(Func<Task> action)
    {
        try
        {
            IsLoading = true;
            await action();
            await LoadCharactersAsync();
        }
        catch (CharacterException ex)
        {
            _logger.LogWarning(ex, "{Message}", ex.Message);
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update character");
            StatusMessage = "Impossibile aggiornare il personaggio.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}