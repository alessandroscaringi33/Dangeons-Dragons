using DndCompanion.Core.Domain.Entities;
using DndCompanion.Core.Npcs;
using DndCompanion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.Npcs;

/// <summary>
/// Manages the non-player characters of a campaign using the campaign's SQLite
/// database. Quick HP operations are single-entity updates; scene/location
/// links are nullable FKs cleared automatically when the target is deleted.
/// </summary>
public sealed class NpcService : INpcService
{
    private readonly ICampaignDatabaseFactory _databaseFactory;
    private readonly ILogger<NpcService> _logger;

    public NpcService(
        ICampaignDatabaseFactory databaseFactory,
        ILogger<NpcService> logger)
    {
        _databaseFactory = databaseFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NpcInfo>> ListNpcsAsync(
        string campaignFolderPath,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        IQueryable<Npc> query = context.Npcs
            .AsNoTracking()
            .Include(n => n.Scene)
            .Include(n => n.Location);

        var term = searchTerm?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var lowered = term.ToLowerInvariant();
            query = query.Where(n =>
                n.Name.ToLower().Contains(lowered) ||
                n.Role.ToLower().Contains(lowered) ||
                n.Description.ToLower().Contains(lowered));
        }

        var npcs = await query
            .OrderBy(n => n.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return npcs.Select(ToInfo).ToList();
    }

    public async Task<NpcInfo> GetNpcAsync(
        string campaignFolderPath,
        Guid npcId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var npc = await context.Npcs
            .AsNoTracking()
            .Include(n => n.Scene)
            .Include(n => n.Location)
            .FirstOrDefaultAsync(n => n.Id == npcId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NpcException("L'NPC non è stato trovato.");

        return ToInfo(npc);
    }

    public async Task<NpcInfo> CreateNpcAsync(
        string campaignFolderPath,
        NpcDraft draft,
        CancellationToken cancellationToken = default)
    {
        Validate(draft);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var campaign = await context.Campaigns
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NpcException("La campagna non è stata trovata o non è inizializzata.");

        var npc = new Npc
        {
            CampaignId = campaign.Id,
            Name = draft.Name.Trim(),
            Role = draft.Role?.Trim() ?? string.Empty,
            Description = draft.Description ?? string.Empty,
            MaxHp = draft.MaxHp,
            CurrentHp = draft.CurrentHp,
            ArmorClass = draft.ArmorClass,
            InitiativeModifier = draft.InitiativeModifier,
            Notes = draft.Notes ?? string.Empty,
            IsKnown = draft.IsKnown,
            IsAlive = draft.CurrentHp > 0,
            SceneId = draft.SceneId,
            LocationId = draft.LocationId
        };

        await ValidateLinksAsync(context, npc, cancellationToken).ConfigureAwait(false);

        context.Npcs.Add(npc);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created NPC '{Name}' in campaign {Folder}", npc.Name, campaignFolderPath);
        return await GetNpcAsync(campaignFolderPath, npc.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<NpcInfo> UpdateNpcAsync(
        string campaignFolderPath,
        Guid npcId,
        NpcDraft draft,
        CancellationToken cancellationToken = default)
    {
        Validate(draft);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var npc = await RequireAsync(context, npcId, cancellationToken).ConfigureAwait(false);

        npc.Name = draft.Name.Trim();
        npc.Role = draft.Role?.Trim() ?? string.Empty;
        npc.Description = draft.Description ?? string.Empty;
        npc.MaxHp = draft.MaxHp;
        npc.CurrentHp = Core.Domain.Rules.Clamp(draft.CurrentHp, 0, draft.MaxHp);
        npc.ArmorClass = draft.ArmorClass;
        npc.InitiativeModifier = draft.InitiativeModifier;
        npc.Notes = draft.Notes ?? string.Empty;
        npc.IsKnown = draft.IsKnown;
        npc.SceneId = draft.SceneId;
        npc.LocationId = draft.LocationId;
        if (npc.CurrentHp <= 0)
        {
            npc.IsAlive = false;
        }

        await ValidateLinksAsync(context, npc, cancellationToken).ConfigureAwait(false);

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated NPC '{Name}' in campaign {Folder}", npc.Name, campaignFolderPath);
        return await GetNpcAsync(campaignFolderPath, npc.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteNpcAsync(
        string campaignFolderPath,
        Guid npcId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var npc = await context.Npcs
            .FirstOrDefaultAsync(n => n.Id == npcId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NpcException("L'NPC non è stato trovato.");

        context.Npcs.Remove(npc);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted NPC '{Name}' from campaign {Folder}", npc.Name, campaignFolderPath);
    }

    public async Task<NpcInfo> ApplyDamageAsync(
        string campaignFolderPath,
        Guid npcId,
        int amount,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var npc = await RequireAsync(context, npcId, cancellationToken).ConfigureAwait(false);
        npc.ApplyDamage(amount);

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Applied {Amount} damage to NPC '{Name}' in {Folder}", amount, npc.Name, campaignFolderPath);

        return await GetNpcAsync(campaignFolderPath, npc.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<NpcInfo> HealAsync(
        string campaignFolderPath,
        Guid npcId,
        int amount,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var npc = await RequireAsync(context, npcId, cancellationToken).ConfigureAwait(false);
        npc.Heal(amount);

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Healed {Amount} HP to NPC '{Name}' in {Folder}", amount, npc.Name, campaignFolderPath);

        return await GetNpcAsync(campaignFolderPath, npc.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<NpcInfo> SetCurrentHpAsync(
        string campaignFolderPath,
        Guid npcId,
        int hp,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var npc = await RequireAsync(context, npcId, cancellationToken).ConfigureAwait(false);
        npc.CurrentHp = Core.Domain.Rules.Clamp(hp, 0, npc.MaxHp);
        if (npc.CurrentHp <= 0)
        {
            npc.IsAlive = false;
        }

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Set NPC '{Name}' current HP to {Hp} in {Folder}", npc.Name, npc.CurrentHp, campaignFolderPath);

        return await GetNpcAsync(campaignFolderPath, npc.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<NpcInfo> SetAliveAsync(
        string campaignFolderPath,
        Guid npcId,
        bool isAlive,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var npc = await RequireAsync(context, npcId, cancellationToken).ConfigureAwait(false);
        npc.IsAlive = isAlive;
        if (isAlive && npc.CurrentHp <= 0)
        {
            npc.CurrentHp = 1;
        }

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Set NPC '{Name}' alive state to {Alive} in {Folder}", npc.Name, isAlive, campaignFolderPath);

        return await GetNpcAsync(campaignFolderPath, npc.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<NpcInfo> LinkToSceneAsync(
        string campaignFolderPath,
        Guid npcId,
        Guid? sceneId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var npc = await RequireAsync(context, npcId, cancellationToken).ConfigureAwait(false);

        if (sceneId is Guid id)
        {
            var exists = await context.Scenes.AnyAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new NpcException("La scena non è stata trovata.");
            }
        }

        npc.SceneId = sceneId;
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Linked NPC '{Name}' to scene {Scene} in {Folder}", npc.Name, sceneId, campaignFolderPath);
        return await GetNpcAsync(campaignFolderPath, npc.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<NpcInfo> LinkToLocationAsync(
        string campaignFolderPath,
        Guid npcId,
        Guid? locationId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var npc = await RequireAsync(context, npcId, cancellationToken).ConfigureAwait(false);

        if (locationId is Guid id)
        {
            var exists = await context.Locations.AnyAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new NpcException("Il luogo non è stato trovato.");
            }
        }

        npc.LocationId = locationId;
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Linked NPC '{Name}' to location {Location} in {Folder}", npc.Name, locationId, campaignFolderPath);
        return await GetNpcAsync(campaignFolderPath, npc.Id, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Npc> RequireAsync(
        CampaignDbContext context,
        Guid npcId,
        CancellationToken cancellationToken)
    {
        return await context.Npcs
            .FirstOrDefaultAsync(n => n.Id == npcId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NpcException("L'NPC non è stato trovato.");
    }

    private static async Task ValidateLinksAsync(
        CampaignDbContext context,
        Npc npc,
        CancellationToken cancellationToken)
    {
        if (npc.SceneId is Guid sceneId)
        {
            var sceneExists = await context.Scenes.AnyAsync(s => s.Id == sceneId, cancellationToken).ConfigureAwait(false);
            if (!sceneExists)
            {
                throw new NpcException("La scena collegata non è stata trovata.");
            }
        }

        if (npc.LocationId is Guid locationId)
        {
            var locationExists = await context.Locations.AnyAsync(l => l.Id == locationId, cancellationToken).ConfigureAwait(false);
            if (!locationExists)
            {
                throw new NpcException("Il luogo collegato non è stato trovato.");
            }
        }
    }

    private static void Validate(NpcDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Name))
        {
            throw new NpcException("Il nome dell'NPC è obbligatorio.");
        }

        if (draft.MaxHp < 0)
        {
            throw new NpcException("Gli HP massimi non possono essere negativi.");
        }

        if (draft.CurrentHp < 0)
        {
            throw new NpcException("Gli HP correnti non possono essere negativi.");
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
            throw new NpcException("Non è stato possibile salvare l'NPC.", ex);
        }
    }

    private static NpcInfo ToInfo(Npc n)
    {
        return new NpcInfo
        {
            Id = n.Id,
            CampaignId = n.CampaignId,
            Name = n.Name,
            Role = n.Role,
            Description = n.Description,
            CurrentHp = n.CurrentHp,
            MaxHp = n.MaxHp,
            ArmorClass = n.ArmorClass,
            InitiativeModifier = n.InitiativeModifier,
            Notes = n.Notes,
            IsAlive = n.IsAlive,
            IsKnown = n.IsKnown,
            SceneId = n.SceneId,
            SceneTitle = n.Scene?.Title,
            LocationId = n.LocationId,
            LocationName = n.Location?.Name
        };
    }
}