using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DndCompanion.Core.Characters;
using DndCompanion.Core.Mvvm;
using Microsoft.Extensions.Logging;

namespace DndCompanion.ViewModels;

/// <summary>
/// View model for the "Personaggi" list screen: shows every character of the
/// opened campaign with quick HP controls. Dialog prompts are handled by the
/// page code-behind.
/// </summary>
public sealed partial class CharactersViewModel : ViewModelBase
{
    private readonly ICharacterService _characterService;
    private readonly ILogger<CharactersViewModel> _logger;

    [ObservableProperty]
    private string campaignName = string.Empty;

    [ObservableProperty]
    private string folderPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CharacterItemViewModel> characters = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasCharacters;

    public CharactersViewModel(
        ICharacterService characterService,
        ILogger<CharactersViewModel> logger)
    {
        _characterService = characterService;
        _logger = logger;
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool HasNoCharacters => !HasCharacters;

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

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
            var characters = await _characterService.ListCharactersAsync(FolderPath);
            Rebuild(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load characters of campaign in {Folder}", FolderPath);
            StatusMessage = "Impossibile caricare i personaggi.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CreateCharacterAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await ExecuteAsync(
            () => _characterService.CreateCharacterAsync(FolderPath, new CharacterDraft { Name = name, MaxHp = 10, CurrentHp = 10 }),
            $"Personaggio '{name.Trim()}' creato.",
            "Impossibile creare il personaggio.");
    }

    public async Task DeleteCharacterAsync(CharacterItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _characterService.DeleteCharacterAsync(FolderPath, item.Id),
            $"Personaggio '{item.Name}' eliminato.",
            "Impossibile eliminare il personaggio.");
    }

    public async Task ApplyDamageAsync(CharacterItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteAsync(
            () => _characterService.ApplyDamageAsync(FolderPath, item.Id, amount),
            null,
            "Impossibile applicare il danno.");
    }

    public async Task HealAsync(CharacterItemViewModel? item, int amount)
    {
        if (item is null || amount <= 0)
        {
            return;
        }

        await ExecuteAsync(
            () => _characterService.HealAsync(FolderPath, item.Id, amount),
            null,
            "Impossibile curare il personaggio.");
    }

    public async Task SetCurrentHpAsync(CharacterItemViewModel? item, int hp)
    {
        if (item is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _characterService.SetCurrentHpAsync(FolderPath, item.Id, hp),
            $"HP di '{item.Name}' impostati a {hp}.",
            "Impossibile impostare gli HP.");
    }

    private void Rebuild(IReadOnlyList<CharacterInfo> characters)
    {
        Characters.Clear();
        foreach (var character in characters)
        {
            Characters.Add(new CharacterItemViewModel(character));
        }

        HasCharacters = Characters.Count > 0;
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
            var characters = await _characterService.ListCharactersAsync(FolderPath);
            Rebuild(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload characters in {Folder}", FolderPath);
        }
    }
}