using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Characters;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.Mvvm;
using DndCompanion.Core.SkillChecks;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the "Nuovo tiro" screen: a DM-facing skill check panel with
/// immediate success/failure feedback and a recent history. Dialog prompts are
/// handled by the page code-behind.
/// </summary>
public sealed partial class SkillCheckViewModel : ViewModelBase
{
    private readonly ISkillCheckService _skillCheckService;
    private readonly ICharacterService _characterService;
    private readonly ILogger<SkillCheckViewModel> _logger;

    private readonly Dictionary<string, Guid> _characterLookup = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> characters = new();

    [ObservableProperty]
    private string selectedCharacterName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> skills = new(SkillList.DefaultSkills);

    [ObservableProperty]
    private string selectedSkill = "Percezione";

    [ObservableProperty]
    private string difficultyClassText = "15";

    [ObservableProperty]
    private ObservableCollection<string> diceTypes = new();

    [ObservableProperty]
    private string selectedDiceType = "1d20";

    [ObservableProperty]
    private string modifierText = "0";

    [ObservableProperty]
    private bool isPhysicalRoll;

    [ObservableProperty]
    private string physicalRollText = string.Empty;

    [ObservableProperty]
    private bool hasOutcome;

    [ObservableProperty]
    private bool isSuccess;

    [ObservableProperty]
    private string outcomeSummary = string.Empty;

    [ObservableProperty]
    private Color outcomeColor = Colors.Gray;

    [ObservableProperty]
    private ObservableCollection<SkillCheckItemViewModel> recentChecks = new();

    [ObservableProperty]
    private bool hasRecentChecks;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SkillCheckViewModel(
        ISkillCheckService skillCheckService,
        ICharacterService characterService,
        ILogger<SkillCheckViewModel> logger)
    {
        _skillCheckService = skillCheckService;
        _characterService = characterService;
        _logger = logger;

        DiceTypes = new ObservableCollection<string>(
            new[] { "d4", "d6", "d8", "d10", "d12", "d20", "d100" });
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool HasNoRecentChecks => !HasRecentChecks;

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

    partial void OnHasRecentChecksChanged(bool value) => OnPropertyChanged(nameof(HasNoRecentChecks));

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
            await LoadCharactersAsync();
            await LoadRecentChecksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load skill check panel for {Folder}", FolderPath);
            StatusMessage = "Impossibile caricare il pannello tiri.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RollAsync()
    {
        var draft = BuildDraft();
        if (draft is null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            var check = await _skillCheckService.RecordCheckAsync(FolderPath, draft);
            ShowOutcome(check);
            await LoadRecentChecksAsync();
        }
        catch (SkillCheckException ex)
        {
            _logger.LogWarning(ex, "{Message}", ex.Message);
            StatusMessage = ex.Message;
            HasOutcome = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record skill check");
            StatusMessage = "Impossibile registrare il tiro.";
            HasOutcome = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ClearOutcome()
    {
        HasOutcome = false;
    }

    private SkillCheckDraft? BuildDraft()
    {
        var dc = Parse(DifficultyClassText);
        var modifier = Parse(ModifierText);
        var notation = $"1{SelectedDiceType}";
        var physicalValue = 0;

        if (IsPhysicalRoll && !int.TryParse(PhysicalRollText, out physicalValue))
        {
            StatusMessage = "Inserisci il valore del dado fisico.";
            return null;
        }

        return new SkillCheckDraft
        {
            CharacterId = _characterLookup.GetValueOrDefault(SelectedCharacterName),
            Skill = SelectedSkill,
            DiceNotation = notation,
            Modifier = modifier,
            DifficultyClass = dc,
            IsPhysicalRoll = IsPhysicalRoll,
            PhysicalRoll = IsPhysicalRoll ? physicalValue : null
        };
    }

    private void ShowOutcome(SkillCheckInfo check)
    {
        IsSuccess = check.IsSuccess;
        HasOutcome = true;
        OutcomeColor = check.IsSuccess ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
        OutcomeSummary = $"{check.Skill} — tiro {check.Roll}, bonus {check.Modifier:+#;-#;0}, " +
                         $"totale {check.Total} (CD {check.DifficultyClass})";
    }

    public string OutcomeText => IsSuccess ? "SUCCESSO" : "FALLIMENTO";

    public Color OutcomeBackgroundColor => Color.FromArgb(IsSuccess ? "#1E2B22" : "#3A1F1F");

    private async Task LoadCharactersAsync()
    {
        _characterLookup.Clear();
        Characters.Clear();
        Characters.Add(string.Empty);
        var characters = await _characterService.ListCharactersAsync(FolderPath);
        foreach (var character in characters)
        {
            Characters.Add(character.Name);
            _characterLookup[character.Name] = character.Id;
        }

        SelectedCharacterName = string.Empty;
    }

    private async Task LoadRecentChecksAsync()
    {
        var checks = await _skillCheckService.ListChecksAsync(FolderPath, limit: 10);
        RecentChecks.Clear();
        foreach (var check in checks)
        {
            RecentChecks.Add(new SkillCheckItemViewModel(check));
        }

        HasRecentChecks = RecentChecks.Count > 0;
    }

    private static int Parse(string value) => int.TryParse(value, out var result) ? result : 0;
}

/// <summary>The standard D&amp;D 5e skill list in Italian.</summary>
public static class SkillList
{
    public static readonly IReadOnlyList<string> DefaultSkills = new[]
    {
        "Acrobazia",
        "Addestrare Animali",
        "Arcano",
        "Atletica",
        "Furtività",
        "Inganno",
        "Indagare",
        "Intimidire",
        "Intrattenere",
        "Intuizione",
        "Medicina",
        "Natura",
        "Percezione",
        "Persuasione",
        "Rapidità di Mano",
        "Religione",
        "Sopravvivenza",
        "Storia"
    };
}

/// <summary>Presentation model of a recorded skill check in the recent list.</summary>
public sealed class SkillCheckItemViewModel : ViewModelBase
{
    public SkillCheckItemViewModel(SkillCheckInfo check)
    {
        Check = check;
    }

    public SkillCheckInfo Check { get; }

    public string Skill => Check.Skill;

    public string? CharacterName => Check.CharacterName;

    public bool HasCharacter => !string.IsNullOrEmpty(CharacterName);

    public string TimeText => Check.Timestamp.ToLocalTime().ToString("HH:mm");

    public string TotalText => $"Totale {Check.Total}";

    public string OutcomeText => Check.OutcomeText;

    public Color OutcomeColor => Check.IsSuccess ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
}