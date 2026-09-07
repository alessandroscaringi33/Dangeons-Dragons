using DndCompanion.Core.Characters;
using DndCompanion.Core.Domain.Entities;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.Characters;

/// <summary>
/// Manages the player characters of a campaign using the campaign's SQLite
/// database. HP/condition/inventory operations are quick, single-entity
/// updates; derived values are always computed from the stored raw fields.
/// </summary>
public sealed class CharacterService : ICharacterService
{
    private readonly ICampaignDatabaseFactory _databaseFactory;
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(
        ICampaignDatabaseFactory databaseFactory,
        ILogger<CharacterService> logger)
    {
        _databaseFactory = databaseFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CharacterInfo>> ListCharactersAsync(
        string campaignFolderPath,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var characters = await context.Characters
            .AsNoTracking()
            .Include(c => c.Inventory)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return characters.Select(ToInfo).ToList();
    }

    public async Task<CharacterInfo> GetCharacterAsync(
        string campaignFolderPath,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var character = await context.Characters
            .AsNoTracking()
            .Include(c => c.Inventory)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CharacterException("Il personaggio non è stato trovato.");

        return ToInfo(character);
    }

    public async Task<CharacterInfo> CreateCharacterAsync(
        string campaignFolderPath,
        CharacterDraft draft,
        CancellationToken cancellationToken = default)
    {
        Validate(draft);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var campaign = await context.Campaigns
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CharacterException("La campagna non è stata trovata o non è inizializzata.");

        var character = new Character
        {
            CampaignId = campaign.Id,
            Name = draft.Name.Trim(),
            PlayerName = draft.PlayerName?.Trim() ?? string.Empty,
            Class = draft.Class?.Trim() ?? string.Empty,
            Subclass = draft.Subclass?.Trim() ?? string.Empty,
            Race = draft.Race?.Trim() ?? string.Empty,
            Background = draft.Background?.Trim() ?? string.Empty,
            Level = draft.Level,
            Experience = draft.Experience,
            Strength = draft.Strength,
            Dexterity = draft.Dexterity,
            Constitution = draft.Constitution,
            Intelligence = draft.Intelligence,
            Wisdom = draft.Wisdom,
            Charisma = draft.Charisma,
            MaxHp = draft.MaxHp,
            CurrentHp = draft.CurrentHp,
            TemporaryHp = draft.TemporaryHp,
            ArmorClass = draft.ArmorClass,
            InitiativeModifier = draft.InitiativeModifier,
            Speed = draft.Speed,
            Notes = draft.Notes ?? string.Empty,
            Conditions = draft.Conditions
        };

        context.Characters.Add(character);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created character '{Name}' in campaign {Folder}", character.Name, campaignFolderPath);
        return ToInfo(character);
    }

    public async Task<CharacterInfo> UpdateCharacterAsync(
        string campaignFolderPath,
        Guid characterId,
        CharacterDraft draft,
        CancellationToken cancellationToken = default)
    {
        Validate(draft);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var character = await context.Characters
            .Include(c => c.Inventory)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CharacterException("Il personaggio non è stato trovato.");

        character.Name = draft.Name.Trim();
        character.PlayerName = draft.PlayerName?.Trim() ?? string.Empty;
        character.Class = draft.Class?.Trim() ?? string.Empty;
        character.Subclass = draft.Subclass?.Trim() ?? string.Empty;
        character.Race = draft.Race?.Trim() ?? string.Empty;
        character.Background = draft.Background?.Trim() ?? string.Empty;
        character.Level = draft.Level;
        character.Experience = draft.Experience;
        character.Strength = draft.Strength;
        character.Dexterity = draft.Dexterity;
        character.Constitution = draft.Constitution;
        character.Intelligence = draft.Intelligence;
        character.Wisdom = draft.Wisdom;
        character.Charisma = draft.Charisma;
        character.MaxHp = draft.MaxHp;
        character.CurrentHp = Core.Domain.Rules.Clamp(draft.CurrentHp, 0, draft.MaxHp);
        character.TemporaryHp = draft.TemporaryHp < 0 ? 0 : draft.TemporaryHp;
        character.ArmorClass = draft.ArmorClass;
        character.InitiativeModifier = draft.InitiativeModifier;
        character.Speed = draft.Speed;
        character.Notes = draft.Notes ?? string.Empty;
        character.Conditions = draft.Conditions;

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated character '{Name}' in campaign {Folder}", character.Name, campaignFolderPath);
        return ToInfo(character);
    }

    public async Task DeleteCharacterAsync(
        string campaignFolderPath,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var character = await context.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CharacterException("Il personaggio non è stato trovato.");

        context.Characters.Remove(character);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted character '{Name}' from campaign {Folder}", character.Name, campaignFolderPath);
    }

    public async Task<CharacterInfo> ApplyDamageAsync(
        string campaignFolderPath,
        Guid characterId,
        int amount,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var character = await RequireAsync(context, characterId, cancellationToken).ConfigureAwait(false);
        character.ApplyDamage(amount);

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Applied {Amount} damage to '{Name}' in {Folder}", amount, character.Name, campaignFolderPath);

        return ToInfo(character);
    }

    public async Task<CharacterInfo> HealAsync(
        string campaignFolderPath,
        Guid characterId,
        int amount,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var character = await RequireAsync(context, characterId, cancellationToken).ConfigureAwait(false);
        character.Heal(amount);

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Healed {Amount} HP to '{Name}' in {Folder}", amount, character.Name, campaignFolderPath);

        return ToInfo(character);
    }

    public async Task<CharacterInfo> SetCurrentHpAsync(
        string campaignFolderPath,
        Guid characterId,
        int hp,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var character = await RequireAsync(context, characterId, cancellationToken).ConfigureAwait(false);
        character.CurrentHp = Core.Domain.Rules.Clamp(hp, 0, character.MaxHp);

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Set '{Name}' current HP to {Hp} in {Folder}", character.Name, character.CurrentHp, campaignFolderPath);

        return ToInfo(character);
    }

    public async Task<CharacterInfo> SetTemporaryHpAsync(
        string campaignFolderPath,
        Guid characterId,
        int hp,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var character = await RequireAsync(context, characterId, cancellationToken).ConfigureAwait(false);
        character.SetTemporaryHp(hp);

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Set '{Name}' temporary HP to {Hp} in {Folder}", character.Name, character.TemporaryHp, campaignFolderPath);

        return ToInfo(character);
    }

    public async Task<CharacterInfo> AddConditionAsync(
        string campaignFolderPath,
        Guid characterId,
        CharacterCondition condition,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var character = await RequireAsync(context, characterId, cancellationToken).ConfigureAwait(false);
        character.AddCondition(condition);

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Added condition {Condition} to '{Name}' in {Folder}", condition, character.Name, campaignFolderPath);

        return ToInfo(character);
    }

    public async Task<CharacterInfo> RemoveConditionAsync(
        string campaignFolderPath,
        Guid characterId,
        CharacterCondition condition,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var character = await RequireAsync(context, characterId, cancellationToken).ConfigureAwait(false);
        character.RemoveCondition(condition);

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Removed condition {Condition} from '{Name}' in {Folder}", condition, character.Name, campaignFolderPath);

        return ToInfo(character);
    }

    public async Task<CharacterInfo> AddInventoryItemAsync(
        string campaignFolderPath,
        Guid characterId,
        string name,
        int quantity,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new CharacterException("Il nome dell'oggetto è obbligatorio.");
        }

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var exists = await context.Characters
            .AnyAsync(c => c.Id == characterId, cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            throw new CharacterException("Il personaggio non è stato trovato.");
        }

        context.InventoryItems.Add(new InventoryItem
        {
            CharacterId = characterId,
            Name = name.Trim(),
            Quantity = quantity < 1 ? 1 : quantity,
            Notes = notes ?? string.Empty
        });

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Added item '{Item}' to character '{Character}' in {Folder}", name.Trim(), characterId, campaignFolderPath);

        return await GetCharacterAsync(campaignFolderPath, characterId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CharacterInfo> RemoveInventoryItemAsync(
        string campaignFolderPath,
        Guid characterId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var exists = await context.Characters
            .AnyAsync(c => c.Id == characterId, cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            throw new CharacterException("Il personaggio non è stato trovato.");
        }

        var item = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CharacterId == characterId, cancellationToken)
            .ConfigureAwait(false);

        if (item is not null)
        {
            context.InventoryItems.Remove(item);
            await SaveAsync(context, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Removed item from character '{Character}' in {Folder}", characterId, campaignFolderPath);
        }

        return await GetCharacterAsync(campaignFolderPath, characterId, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Character> RequireAsync(
        CampaignDbContext context,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        return await context.Characters
            .Include(c => c.Inventory)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CharacterException("Il personaggio non è stato trovato.");
    }

    private static void Validate(CharacterDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Name))
        {
            throw new CharacterException("Il nome del personaggio è obbligatorio.");
        }

        if (draft.Level < 1)
        {
            throw new CharacterException("Il livello deve essere almeno 1.");
        }

        if (draft.MaxHp < 0)
        {
            throw new CharacterException("Gli HP massimi non possono essere negativi.");
        }

        if (draft.CurrentHp < 0)
        {
            throw new CharacterException("Gli HP correnti non possono essere negativi.");
        }
    }

    private static async Task SaveAsync(CampaignDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new CharacterException("Non è stato possibile salvare il personaggio.", ex);
        }
    }

    private static CharacterInfo ToInfo(Character c)
    {
        return new CharacterInfo
        {
            Id = c.Id,
            CampaignId = c.CampaignId,
            Name = c.Name,
            PlayerName = c.PlayerName,
            Class = c.Class,
            Subclass = c.Subclass,
            Race = c.Race,
            Background = c.Background,
            Level = c.Level,
            Experience = c.Experience,
            Strength = c.Strength,
            Dexterity = c.Dexterity,
            Constitution = c.Constitution,
            Intelligence = c.Intelligence,
            Wisdom = c.Wisdom,
            Charisma = c.Charisma,
            CurrentHp = c.CurrentHp,
            MaxHp = c.MaxHp,
            TemporaryHp = c.TemporaryHp,
            ArmorClass = c.ArmorClass,
            InitiativeModifier = c.InitiativeModifier,
            Speed = c.Speed,
            Notes = c.Notes,
            Conditions = c.Conditions,
            Inventory = c.Inventory
                .OrderBy(i => i.Name)
                .Select(i => new InventoryItemInfo
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Weight = i.Weight,
                    Notes = i.Notes
                })
                .ToList()
        };
    }
}