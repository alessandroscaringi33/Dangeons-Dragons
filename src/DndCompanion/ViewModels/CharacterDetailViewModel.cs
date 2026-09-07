using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Characters;
using DndCompanion.Core.Domain;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.Mvvm;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the character sheet detail screen. Raw fields are editable
/// through the page while derived values (modifiers, proficiency bonus,
/// initiative, passive perception) are computed live and never stored.
/// </summary>
public sealed partial class CharacterDetailViewModel : ViewModelBase
{
    private readonly ICharacterService _characterService;
    private readonly ILogger<CharacterDetailViewModel> _logger;

    private Guid _characterId;
    private CharacterInfo? _current;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string playerName = string.Empty;

    [ObservableProperty]
    private string race = string.Empty;

    [ObservableProperty]
    private string characterClass = string.Empty;

    [ObservableProperty]
    private string subclass = string.Empty;

    [ObservableProperty]
    private string background = string.Empty;

    [ObservableProperty]
    private string levelText = "1";

    [ObservableProperty]
    private string experienceText = "0";

    [ObservableProperty]
    private string strengthText = "10";

    [ObservableProperty]
    private string dexterityText = "10";

    [ObservableProperty]
    private string constitutionText = "10";

    [ObservableProperty]
    private string intelligenceText = "10";

    [ObservableProperty]
    private string wisdomText = "10";

    [ObservableProperty]
    private string charismaText = "10";

    [ObservableProperty]
    private string maxHpText = "0";

    [ObservableProperty]
    private string currentHpText = "0";

    [ObservableProperty]
    private string temporaryHpText = "0";

    [ObservableProperty]
    private string armorClassText = "0";

    [ObservableProperty]
    private string initiativeText = "0";

    [ObservableProperty]
    private string speedText = "0";

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> activeConditions = new();

    [ObservableProperty]
    private ObservableCollection<ConditionItemViewModel> conditions = new();

    [ObservableProperty]
    private ObservableCollection<InventoryItemInfo> inventory = new();

    [ObservableProperty]
    private bool hasInventory;

    public bool HasNoInventory => !HasInventory;

    partial void OnHasInventoryChanged(bool value) => OnPropertyChanged(nameof(HasNoInventory));

    public CharacterDetailViewModel(
        ICharacterService characterService,
        ILogger<CharacterDetailViewModel> logger)
    {
        _characterService = characterService;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

    public void SetCharacterId(Guid characterId) => _characterId = characterId;

    // Live-derived values, recomputed whenever a raw field changes.

    public int StrengthModifier => Rules.Modifier(Parse(StrengthText));
    public int DexterityModifier => Rules.Modifier(Parse(DexterityText));
    public int ConstitutionModifier => Rules.Modifier(Parse(ConstitutionText));
    public int IntelligenceModifier => Rules.Modifier(Parse(IntelligenceText));
    public int WisdomModifier => Rules.Modifier(Parse(WisdomText));
    public int CharismaModifier => Rules.Modifier(Parse(CharismaText));

    public int ProficiencyBonus => Rules.ProficiencyBonus(Parse(LevelText));

    public int InitiativeTotal
    {
        get
        {
            var stored = Parse(InitiativeText);
            return stored == 0 ? DexterityModifier : stored;
        }
    }

    public int PassivePerception => Rules.PassivePerception(WisdomModifier);

    public int Hp => Parse(CurrentHpText);
    public int MaxHp => Parse(MaxHpText);

    partial void OnStrengthTextChanged(string value) => OnPropertyChanged(nameof(StrengthModifier));
    partial void OnDexterityTextChanged(string value)
    {
        OnPropertyChanged(nameof(DexterityModifier));
        OnPropertyChanged(nameof(InitiativeTotal));
    }

    partial void OnConstitutionTextChanged(string value) => OnPropertyChanged(nameof(ConstitutionModifier));
    partial void OnIntelligenceTextChanged(string value) => OnPropertyChanged(nameof(IntelligenceModifier));
    partial void OnWisdomTextChanged(string value)
    {
        OnPropertyChanged(nameof(WisdomModifier));
        OnPropertyChanged(nameof(PassivePerception));
    }

    partial void OnCharismaTextChanged(string value) => OnPropertyChanged(nameof(CharismaModifier));

    partial void OnLevelTextChanged(string value)
    {
        OnPropertyChanged(nameof(ProficiencyBonus));
    }

    partial void OnInitiativeTextChanged(string value) => OnPropertyChanged(nameof(InitiativeTotal));

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
            var character = await _characterService.GetCharacterAsync(FolderPath, _characterId);
            Apply(character);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load character {Character} in {Folder}", _characterId, FolderPath);
            StatusMessage = "Impossibile caricare il personaggio.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        var draft = BuildDraft();
        if (draft is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _characterService.UpdateCharacterAsync(FolderPath, _characterId, draft),
            "Personaggio salvato.",
            "Impossibile salvare il personaggio.");
    }

    public async Task ApplyDamageAsync(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        await ExecuteAsync(
            () => _characterService.ApplyDamageAsync(FolderPath, _characterId, amount),
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
            () => _characterService.HealAsync(FolderPath, _characterId, amount),
            null,
            "Impossibile curare il personaggio.");
    }

    public async Task SetCurrentHpAsync(int hp)
    {
        await ExecuteAsync(
            () => _characterService.SetCurrentHpAsync(FolderPath, _characterId, hp),
            "HP impostati.",
            "Impossibile impostare gli HP.");
    }

    public async Task SetTemporaryHpAsync(int hp)
    {
        await ExecuteAsync(
            () => _characterService.SetTemporaryHpAsync(FolderPath, _characterId, hp),
            "HP temporanei impostati.",
            "Impossibile impostare gli HP temporanei.");
    }

    public async Task ToggleConditionAsync(string conditionName)
    {
        var condition = ParseCondition(conditionName);
        if (condition == CharacterCondition.None)
        {
            return;
        }

        var has = _current is not null && (_current.Conditions & condition) == condition;

        Func<Task> action = has
            ? () => _characterService.RemoveConditionAsync(FolderPath, _characterId, condition)
            : () => _characterService.AddConditionAsync(FolderPath, _characterId, condition);

        await ExecuteAsync(action, null, "Impossibile aggiornare le condizioni.");
    }

    public async Task AddInventoryItemAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await ExecuteAsync(
            () => _characterService.AddInventoryItemAsync(FolderPath, _characterId, name, 1),
            $"Oggetto '{name.Trim()}' aggiunto.",
            "Impossibile aggiungere l'oggetto.");
    }

    public async Task RemoveInventoryItemAsync(InventoryItemInfo? item)
    {
        if (item is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _characterService.RemoveInventoryItemAsync(FolderPath, _characterId, item.Id),
            $"Oggetto '{item.Name}' rimosso.",
            "Impossibile rimuovere l'oggetto.");
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
        catch (CharacterException ex)
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
            var character = await _characterService.GetCharacterAsync(FolderPath, _characterId);
            Apply(character);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload character {Character}", _characterId);
        }
    }

    private void Apply(CharacterInfo character)
    {
        _current = character;
        Name = character.Name;
        PlayerName = character.PlayerName;
        Race = character.Race;
        CharacterClass = character.Class;
        Subclass = character.Subclass;
        Background = character.Background;
        LevelText = character.Level.ToString();
        ExperienceText = character.Experience.ToString();
        StrengthText = character.Strength.ToString();
        DexterityText = character.Dexterity.ToString();
        ConstitutionText = character.Constitution.ToString();
        IntelligenceText = character.Intelligence.ToString();
        WisdomText = character.Wisdom.ToString();
        CharismaText = character.Charisma.ToString();
        MaxHpText = character.MaxHp.ToString();
        CurrentHpText = character.CurrentHp.ToString();
        TemporaryHpText = character.TemporaryHp.ToString();
        ArmorClassText = character.ArmorClass.ToString();
        InitiativeText = character.InitiativeModifier.ToString();
        SpeedText = character.Speed.ToString();
        Notes = character.Notes;
        RefreshConditions(character.Conditions);
        Inventory.Clear();
        foreach (var item in character.Inventory)
        {
            Inventory.Add(item);
        }

        HasInventory = Inventory.Count > 0;
        OnPropertyChanged(nameof(InitiativeTotal));
        OnPropertyChanged(nameof(PassivePerception));
        OnPropertyChanged(nameof(ProficiencyBonus));
    }

    private CharacterDraft? BuildDraft()
    {
        var level = Parse(LevelText);
        var maxHp = Parse(MaxHpText);

        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Il nome del personaggio è obbligatorio.";
            return null;
        }

        return new CharacterDraft
        {
            Name = Name,
            PlayerName = PlayerName,
            Class = CharacterClass,
            Subclass = Subclass,
            Race = Race,
            Background = Background,
            Level = level < 1 ? 1 : level,
            Experience = Parse(ExperienceText),
            Strength = Parse(StrengthText),
            Dexterity = Parse(DexterityText),
            Constitution = Parse(ConstitutionText),
            Intelligence = Parse(IntelligenceText),
            Wisdom = Parse(WisdomText),
            Charisma = Parse(CharismaText),
            CurrentHp = Parse(CurrentHpText),
            MaxHp = maxHp,
            TemporaryHp = Parse(TemporaryHpText),
            ArmorClass = Parse(ArmorClassText),
            InitiativeModifier = Parse(InitiativeText),
            Speed = Parse(SpeedText),
            Notes = Notes,
            Conditions = _current?.Conditions ?? CharacterCondition.None
        };
    }

    private void RefreshConditions(CharacterCondition conditions)
    {
        ActiveConditions.Clear();
        Conditions.Clear();

        foreach (var condition in Enum.GetValues<CharacterCondition>())
        {
            if (condition == CharacterCondition.None)
            {
                continue;
            }

            var isActive = (conditions & condition) == condition;
            Conditions.Add(new ConditionItemViewModel(ToItalian(condition), isActive));
            if (isActive)
            {
                ActiveConditions.Add(ToItalian(condition));
            }
        }
    }

    private static int Parse(string value) => int.TryParse(value, out var result) ? result : 0;

    private static string ToItalian(CharacterCondition condition) => condition switch
    {
        CharacterCondition.Blinded => "Cieco",
        CharacterCondition.Charmed => "Amaliato",
        CharacterCondition.Deafened => "Assordato",
        CharacterCondition.Frightened => "Impaurito",
        CharacterCondition.Grappled => "Afferrato",
        CharacterCondition.Incapacitated => "Incapacitato",
        CharacterCondition.Invisible => "Invisibile",
        CharacterCondition.Paralyzed => "Paralizzato",
        CharacterCondition.Petrified => "Pietrificato",
        CharacterCondition.Poisoned => "Avvelenato",
        CharacterCondition.Prone => "Prono",
        CharacterCondition.Restrained => "Trattenuto",
        CharacterCondition.Stunned => "Stordito",
        CharacterCondition.Unconscious => "Incosciente",
        CharacterCondition.Exhaustion => "Affaticamento",
        _ => condition.ToString()
    };

    private static CharacterCondition ParseCondition(string name)
    {
        foreach (var condition in Enum.GetValues<CharacterCondition>())
        {
            if (condition != CharacterCondition.None && ToItalian(condition) == name)
            {
                return condition;
            }
        }

        return CharacterCondition.None;
    }
}