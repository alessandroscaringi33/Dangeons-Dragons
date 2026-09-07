using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Locations;
using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Npcs;
using DndCompanion.Core.Story;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the NPC detail screen. Raw fields are editable while the
/// page offers quick HP and alive/dead controls and scene/location linking.
/// </summary>
public sealed partial class NpcDetailViewModel : ViewModelBase
{
    private readonly INpcService _npcService;
    private readonly IChapterService _chapterService;
    private readonly ILocationService _locationService;
    private readonly ILogger<NpcDetailViewModel> _logger;

    private Guid _npcId;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string role = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string maxHpText = "0";

    [ObservableProperty]
    private string currentHpText = "0";

    [ObservableProperty]
    private string armorClassText = "0";

    [ObservableProperty]
    private string initiativeText = "0";

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private bool isKnown;

    [ObservableProperty]
    private bool isAlive = true;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> scenes = new();

    [ObservableProperty]
    private ObservableCollection<string> locations = new();

    [ObservableProperty]
    private string selectedSceneTitle = string.Empty;

    [ObservableProperty]
    private string selectedLocationName = string.Empty;

    [ObservableProperty]
    private bool hasSceneLink;

    [ObservableProperty]
    private bool hasLocationLink;

    public NpcDetailViewModel(
        INpcService npcService,
        IChapterService chapterService,
        ILocationService locationService,
        ILogger<NpcDetailViewModel> logger)
    {
        _npcService = npcService;
        _chapterService = chapterService;
        _locationService = locationService;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool HasNoSceneLink => !HasSceneLink;

    public bool HasNoLocationLink => !HasLocationLink;

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));
    partial void OnHasSceneLinkChanged(bool value) => OnPropertyChanged(nameof(HasNoSceneLink));
    partial void OnHasLocationLinkChanged(bool value) => OnPropertyChanged(nameof(HasNoLocationLink));

    public void SetNpcId(Guid npcId) => _npcId = npcId;

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
            await LoadOptionsAsync();
            var npc = await _npcService.GetNpcAsync(FolderPath, _npcId);
            Apply(npc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load NPC {Npc} in {Folder}", _npcId, FolderPath);
            StatusMessage = "Impossibile caricare l'NPC.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Il nome dell'NPC è obbligatorio.";
            return;
        }

        var draft = new NpcDraft
        {
            Name = Name,
            Role = Role,
            Description = Description,
            MaxHp = Parse(MaxHpText),
            CurrentHp = Parse(CurrentHpText),
            ArmorClass = Parse(ArmorClassText),
            InitiativeModifier = Parse(InitiativeText),
            Notes = Notes,
            IsKnown = IsKnown,
            SceneId = LookupSceneId(SelectedSceneTitle),
            LocationId = LookupLocationId(SelectedLocationName)
        };

        await ExecuteAsync(
            () => _npcService.UpdateNpcAsync(FolderPath, _npcId, draft),
            "NPC salvato.",
            "Impossibile salvare l'NPC.");
    }

    public async Task ApplyDamageAsync(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.ApplyDamageAsync(FolderPath, _npcId, amount),
            null,
            "Impossibile applicare il danno.");
    }

    public async Task HealAsync(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.HealAsync(FolderPath, _npcId, amount),
            null,
            "Impossibile curare l'NPC.");
    }

    public async Task SetCurrentHpAsync(int hp)
    {
        await ExecuteAsync(
            () => _npcService.SetCurrentHpAsync(FolderPath, _npcId, hp),
            "HP impostati.",
            "Impossibile impostare gli HP.");
    }

    public async Task ToggleAliveAsync()
    {
        await ExecuteAsync(
            () => _npcService.SetAliveAsync(FolderPath, _npcId, !IsAlive),
            null,
            "Impossibile aggiornare lo stato.");
    }

    public async Task LinkToSceneAsync(string sceneTitle)
    {
        var sceneId = LookupSceneId(sceneTitle);
        if (sceneId is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.LinkToSceneAsync(FolderPath, _npcId, sceneId),
            "NPC collegato alla scena.",
            "Impossibile collegare la scena.");
    }

    public async Task ClearSceneLinkAsync()
    {
        await ExecuteAsync(
            () => _npcService.LinkToSceneAsync(FolderPath, _npcId, null),
            "Collegamento scena rimosso.",
            "Impossibile rimuovere il collegamento.");
    }

    public async Task LinkToLocationAsync(string locationName)
    {
        var locationId = LookupLocationId(locationName);
        if (locationId is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _npcService.LinkToLocationAsync(FolderPath, _npcId, locationId),
            "NPC collegato al luogo.",
            "Impossibile collegare il luogo.");
    }

    public async Task ClearLocationLinkAsync()
    {
        await ExecuteAsync(
            () => _npcService.LinkToLocationAsync(FolderPath, _npcId, null),
            "Collegamento luogo rimosso.",
            "Impossibile rimuovere il collegamento.");
    }

    private readonly Dictionary<string, Guid> _sceneLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid> _locationLookup = new(StringComparer.OrdinalIgnoreCase);

    private Guid? LookupSceneId(string title)
    {
        return !string.IsNullOrEmpty(title) && _sceneLookup.TryGetValue(title, out var id) ? id : null;
    }

    private Guid? LookupLocationId(string name)
    {
        return !string.IsNullOrEmpty(name) && _locationLookup.TryGetValue(name, out var id) ? id : null;
    }

    private async Task LoadOptionsAsync()
    {
        _sceneLookup.Clear();
        Scenes.Clear();
        Scenes.Add(string.Empty);
        var chapters = await _chapterService.ListChaptersAsync(FolderPath);
        foreach (var chapter in chapters)
        {
            foreach (var scene in chapter.Scenes)
            {
                Scenes.Add(scene.Title);
                _sceneLookup[scene.Title] = scene.Id;
            }
        }

        _locationLookup.Clear();
        Locations.Clear();
        Locations.Add(string.Empty);
        var locations = await _locationService.ListLocationsAsync(FolderPath);
        foreach (var location in locations)
        {
            Locations.Add(location.Name);
            _locationLookup[location.Name] = location.Id;
        }
    }

    private void Apply(NpcInfo npc)
    {
        Name = npc.Name;
        Role = npc.Role;
        Description = npc.Description;
        MaxHpText = npc.MaxHp.ToString();
        CurrentHpText = npc.CurrentHp.ToString();
        ArmorClassText = npc.ArmorClass.ToString();
        InitiativeText = npc.InitiativeModifier.ToString();
        Notes = npc.Notes;
        IsKnown = npc.IsKnown;
        IsAlive = npc.IsAlive;

        SelectedSceneTitle = npc.SceneTitle ?? string.Empty;
        HasSceneLink = npc.SceneId is not null;

        SelectedLocationName = npc.LocationName ?? string.Empty;
        HasLocationLink = npc.LocationId is not null;
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
            var npc = await _npcService.GetNpcAsync(FolderPath, _npcId);
            Apply(npc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload NPC {Npc}", _npcId);
        }
    }

    private static int Parse(string value) => int.TryParse(value, out var result) ? result : 0;
}