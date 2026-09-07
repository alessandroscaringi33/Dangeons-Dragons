using DndCompanion.Core.Domain.Entities;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.Quests;
using DndCompanion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.Quests;

/// <summary>
/// Manages the quests of a campaign using the campaign's SQLite database.
/// A quest can be linked to a chapter, scene, location and NPC; links are
/// cleared automatically when the linked entity is deleted (SetNull FK).
/// </summary>
public sealed class QuestService : IQuestService
{
    private readonly ICampaignDatabaseFactory _databaseFactory;
    private readonly ILogger<QuestService> _logger;

    public QuestService(
        ICampaignDatabaseFactory databaseFactory,
        ILogger<QuestService> logger)
    {
        _databaseFactory = databaseFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<QuestInfo>> ListQuestsAsync(
        string campaignFolderPath,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        IQueryable<Quest> query = context.Quests
            .AsNoTracking()
            .Include(q => q.Chapter)
            .Include(q => q.Scene)
            .Include(q => q.Location)
            .Include(q => q.Npc);

        var term = searchTerm?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var lowered = term.ToLowerInvariant();
            query = query.Where(q =>
                q.Title.ToLower().Contains(lowered) ||
                q.Description.ToLower().Contains(lowered));
        }

        var quests = await query
            .OrderBy(q => q.Status)
            .ThenBy(q => q.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return quests.Select(ToInfo).ToList();
    }

    public async Task<QuestInfo> GetQuestAsync(
        string campaignFolderPath,
        Guid questId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var quest = await context.Quests
            .AsNoTracking()
            .Include(q => q.Chapter)
            .Include(q => q.Scene)
            .Include(q => q.Location)
            .Include(q => q.Npc)
            .FirstOrDefaultAsync(q => q.Id == questId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new QuestException("La quest non è stata trovata.");

        return ToInfo(quest);
    }

    public async Task<QuestInfo> CreateQuestAsync(
        string campaignFolderPath,
        QuestDraft draft,
        CancellationToken cancellationToken = default)
    {
        Validate(draft);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var campaign = await context.Campaigns
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new QuestException("La campagna non è stata trovata o non è inizializzata.");

        var quest = new Quest
        {
            CampaignId = campaign.Id,
            Title = draft.Title.Trim(),
            Description = draft.Description ?? string.Empty,
            Status = draft.Status,
            Notes = draft.Notes ?? string.Empty,
            ChapterId = draft.ChapterId,
            SceneId = draft.SceneId,
            LocationId = draft.LocationId,
            NpcId = draft.NpcId
        };

        await ValidateLinksAsync(context, quest, cancellationToken).ConfigureAwait(false);

        context.Quests.Add(quest);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created quest '{Title}' ({Status}) in campaign {Folder}", quest.Title, quest.Status, campaignFolderPath);
        return await GetQuestAsync(campaignFolderPath, quest.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<QuestInfo> UpdateQuestAsync(
        string campaignFolderPath,
        Guid questId,
        QuestDraft draft,
        CancellationToken cancellationToken = default)
    {
        Validate(draft);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var quest = await RequireAsync(context, questId, cancellationToken).ConfigureAwait(false);

        quest.Title = draft.Title.Trim();
        quest.Description = draft.Description ?? string.Empty;
        quest.Status = draft.Status;
        quest.Notes = draft.Notes ?? string.Empty;
        quest.ChapterId = draft.ChapterId;
        quest.SceneId = draft.SceneId;
        quest.LocationId = draft.LocationId;
        quest.NpcId = draft.NpcId;

        await ValidateLinksAsync(context, quest, cancellationToken).ConfigureAwait(false);

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated quest '{Title}' in campaign {Folder}", quest.Title, campaignFolderPath);
        return await GetQuestAsync(campaignFolderPath, quest.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteQuestAsync(
        string campaignFolderPath,
        Guid questId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var quest = await context.Quests
            .FirstOrDefaultAsync(q => q.Id == questId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new QuestException("La quest non è stata trovata.");

        context.Quests.Remove(quest);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted quest '{Title}' from campaign {Folder}", quest.Title, campaignFolderPath);
    }

    public async Task<QuestInfo> SetStatusAsync(
        string campaignFolderPath,
        Guid questId,
        QuestStatus status,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var quest = await RequireAsync(context, questId, cancellationToken).ConfigureAwait(false);
        quest.Status = status;

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Set quest '{Title}' status to {Status} in {Folder}", quest.Title, status, campaignFolderPath);
        return await GetQuestAsync(campaignFolderPath, quest.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<QuestInfo> LinkToChapterAsync(
        string campaignFolderPath,
        Guid questId,
        Guid? chapterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var quest = await RequireAsync(context, questId, cancellationToken).ConfigureAwait(false);

        if (chapterId is Guid id)
        {
            var exists = await context.Chapters.AnyAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new QuestException("Il capitolo non è stato trovato.");
            }
        }

        quest.ChapterId = chapterId;
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Linked quest '{Title}' to chapter {Chapter} in {Folder}", quest.Title, chapterId, campaignFolderPath);
        return await GetQuestAsync(campaignFolderPath, quest.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<QuestInfo> LinkToSceneAsync(
        string campaignFolderPath,
        Guid questId,
        Guid? sceneId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var quest = await RequireAsync(context, questId, cancellationToken).ConfigureAwait(false);

        if (sceneId is Guid id)
        {
            var exists = await context.Scenes.AnyAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new QuestException("La scena non è stata trovata.");
            }
        }

        quest.SceneId = sceneId;
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Linked quest '{Title}' to scene {Scene} in {Folder}", quest.Title, sceneId, campaignFolderPath);
        return await GetQuestAsync(campaignFolderPath, quest.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<QuestInfo> LinkToLocationAsync(
        string campaignFolderPath,
        Guid questId,
        Guid? locationId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var quest = await RequireAsync(context, questId, cancellationToken).ConfigureAwait(false);

        if (locationId is Guid id)
        {
            var exists = await context.Locations.AnyAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new QuestException("Il luogo non è stato trovato.");
            }
        }

        quest.LocationId = locationId;
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Linked quest '{Title}' to location {Location} in {Folder}", quest.Title, locationId, campaignFolderPath);
        return await GetQuestAsync(campaignFolderPath, quest.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<QuestInfo> LinkToNpcAsync(
        string campaignFolderPath,
        Guid questId,
        Guid? npcId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var quest = await RequireAsync(context, questId, cancellationToken).ConfigureAwait(false);

        if (npcId is Guid id)
        {
            var exists = await context.Npcs.AnyAsync(n => n.Id == id, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new QuestException("L'NPC non è stato trovato.");
            }
        }

        quest.NpcId = npcId;
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Linked quest '{Title}' to NPC {Npc} in {Folder}", quest.Title, npcId, campaignFolderPath);
        return await GetQuestAsync(campaignFolderPath, quest.Id, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Quest> RequireAsync(
        CampaignDbContext context,
        Guid questId,
        CancellationToken cancellationToken)
    {
        return await context.Quests
            .FirstOrDefaultAsync(q => q.Id == questId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new QuestException("La quest non è stata trovata.");
    }

    private static async Task ValidateLinksAsync(
        CampaignDbContext context,
        Quest quest,
        CancellationToken cancellationToken)
    {
        if (quest.ChapterId is Guid chapterId)
        {
            var exists = await context.Chapters.AnyAsync(c => c.Id == chapterId, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new QuestException("Il capitolo collegato non è stato trovato.");
            }
        }

        if (quest.SceneId is Guid sceneId)
        {
            var exists = await context.Scenes.AnyAsync(s => s.Id == sceneId, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new QuestException("La scena collegata non è stata trovata.");
            }
        }

        if (quest.LocationId is Guid locationId)
        {
            var exists = await context.Locations.AnyAsync(l => l.Id == locationId, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new QuestException("Il luogo collegato non è stato trovato.");
            }
        }

        if (quest.NpcId is Guid npcId)
        {
            var exists = await context.Npcs.AnyAsync(n => n.Id == npcId, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new QuestException("L'NPC collegato non è stato trovato.");
            }
        }
    }

    private static void Validate(QuestDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Title))
        {
            throw new QuestException("Il titolo della quest è obbligatorio.");
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
            throw new QuestException("Non è stato possibile salvare la quest.", ex);
        }
    }

    private static QuestInfo ToInfo(Quest q)
    {
        return new QuestInfo
        {
            Id = q.Id,
            CampaignId = q.CampaignId,
            Title = q.Title,
            Description = q.Description,
            Status = q.Status,
            Notes = q.Notes,
            ChapterId = q.ChapterId,
            ChapterTitle = q.Chapter?.Title,
            SceneId = q.SceneId,
            SceneTitle = q.Scene?.Title,
            LocationId = q.LocationId,
            LocationName = q.Location?.Name,
            NpcId = q.NpcId,
            NpcName = q.Npc?.Name
        };
    }
}