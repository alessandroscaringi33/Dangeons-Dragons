using DndCompanion.Core.Domain.Entities;
using DndCompanion.Core.Story;
using DndCompanion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.Story;

/// <summary>
/// Manages the chapters of a campaign story using the campaign's SQLite
/// database. Ordering is 1-based and kept contiguous after every mutation.
/// </summary>
public sealed class ChapterService : IChapterService
{
    private const int TemporaryOrderOffset = 1_000_000;

    private readonly ICampaignDatabaseFactory _databaseFactory;
    private readonly ILogger<ChapterService> _logger;

    public ChapterService(
        ICampaignDatabaseFactory databaseFactory,
        ILogger<ChapterService> logger)
    {
        _databaseFactory = databaseFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ChapterInfo>> ListChaptersAsync(
        string campaignFolderPath,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var currentSceneId = await context.Campaigns
            .Select(c => c.CurrentSceneId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var chapters = await context.Chapters
            .AsNoTracking()
            .Include(c => c.Scenes)
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return chapters
            .Select(c => ToInfo(c, currentSceneId))
            .ToList();
    }

    public async Task<ChapterInfo> CreateChapterAsync(
        string campaignFolderPath,
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = ValidateTitle(title);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var nextOrder = (await context.Chapters.CountAsync(cancellationToken).ConfigureAwait(false)) + 1;
        var chapter = new Chapter
        {
            CampaignId = await RequireCampaignIdAsync(context, campaignFolderPath, cancellationToken).ConfigureAwait(false),
            Title = normalizedTitle,
            Description = description ?? string.Empty,
            Order = nextOrder
        };

        context.Chapters.Add(chapter);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created chapter '{Title}' (order {Order}) in campaign {Folder}", chapter.Title, chapter.Order, campaignFolderPath);
        return ToInfo(chapter, null);
    }

    public async Task<ChapterInfo> UpdateChapterAsync(
        string campaignFolderPath,
        Guid chapterId,
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = ValidateTitle(title);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var chapter = await context.Chapters
            .FirstOrDefaultAsync(c => c.Id == chapterId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new StoryException("Il capitolo non è stato trovato.");

        chapter.Title = normalizedTitle;
        chapter.Description = description ?? string.Empty;

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated chapter '{Title}' in campaign {Folder}", chapter.Title, campaignFolderPath);
        return ToInfo(chapter, await GetCurrentSceneIdAsync(context, cancellationToken).ConfigureAwait(false));
    }

    public async Task DeleteChapterAsync(
        string campaignFolderPath,
        Guid chapterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var chapter = await context.Chapters
            .Include(c => c.Scenes)
            .FirstOrDefaultAsync(c => c.Id == chapterId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new StoryException("Il capitolo non è stato trovato.");

        await ClearCurrentSceneIfNeededAsync(context, chapter, cancellationToken).ConfigureAwait(false);

        context.Chapters.Remove(chapter);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        await RenumberChaptersAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted chapter '{Title}' from campaign {Folder}", chapter.Title, campaignFolderPath);
    }

    public async Task<ChapterInfo> MoveChapterAsync(
        string campaignFolderPath,
        Guid chapterId,
        int newOrder,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var chapters = await context.Chapters
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var chapter = chapters.FirstOrDefault(c => c.Id == chapterId)
            ?? throw new StoryException("Il capitolo non è stato trovato.");

        chapters.Remove(chapter);

        var targetIndex = Math.Clamp(newOrder - 1, 0, chapters.Count);
        chapters.Insert(targetIndex, chapter);

        await AssignContiguousOrdersAsync(context, chapters, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Moved chapter '{Title}' to position {Order} in campaign {Folder}", chapter.Title, chapter.Order, campaignFolderPath);
        return ToInfo(chapter, await GetCurrentSceneIdAsync(context, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<Guid> RequireCampaignIdAsync(
        CampaignDbContext context,
        string campaignFolderPath,
        CancellationToken cancellationToken)
    {
        var campaign = await context.Campaigns
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new StoryException("La campagna non è stata trovata o non è inizializzata.");

        return campaign.Id;
    }

    private static async Task<Guid?> GetCurrentSceneIdAsync(
        CampaignDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Campaigns
            .Select(c => c.CurrentSceneId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task RenumberChaptersAsync(CampaignDbContext context, CancellationToken cancellationToken)
    {
        var chapters = await context.Chapters
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await AssignContiguousOrdersAsync(context, chapters, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Rewrites the <see cref="Chapter.Order"/> of the given chapters to
    /// contiguous 1..N values. Because <c>(CampaignId, Order)</c> is unique,
    /// orders are first shifted to large non-conflicting temporary values and
    /// then each final value is written with its own
    /// <see cref="CampaignDbContext.SaveChangesAsync"/> so EF Core never has to
    /// order conflicting updates in a single batch.
    /// </summary>
    private static async Task AssignContiguousOrdersAsync(
        CampaignDbContext context,
        List<Chapter> chapters,
        CancellationToken cancellationToken)
    {
        if (chapters.Count == 0)
        {
            return;
        }

        for (var i = 0; i < chapters.Count; i++)
        {
            chapters[i].Order += TemporaryOrderOffset;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < chapters.Count; i++)
        {
            chapters[i].Order = i + 1;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ClearCurrentSceneIfNeededAsync(
        CampaignDbContext context,
        Chapter chapter,
        CancellationToken cancellationToken)
    {
        var sceneIds = chapter.Scenes.Select(s => s.Id).ToHashSet();
        if (sceneIds.Count == 0)
        {
            return;
        }

        var campaign = await context.Campaigns
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (campaign is not null && campaign.CurrentSceneId is Guid current && sceneIds.Contains(current))
        {
            campaign.CurrentSceneId = null;
            campaign.Touch();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
            throw new StoryException("Non è stato possibile salvare il capitolo.", ex);
        }
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new StoryException("Il titolo del capitolo è obbligatorio.");
        }

        return title.Trim();
    }

    private static ChapterInfo ToInfo(Chapter chapter, Guid? currentSceneId)
    {
        return new ChapterInfo
        {
            Id = chapter.Id,
            CampaignId = chapter.CampaignId,
            Title = chapter.Title,
            Description = chapter.Description,
            Order = chapter.Order,
            Notes = chapter.Notes,
            Scenes = chapter.Scenes
                .OrderBy(s => s.Order)
                .Select(s => new SceneInfo
                {
                    Id = s.Id,
                    ChapterId = s.ChapterId,
                    Title = s.Title,
                    Description = s.Description,
                    Order = s.Order,
                    IsCompleted = s.IsCompleted,
                    Notes = s.Notes
                })
                .ToList(),
            CurrentSceneId = currentSceneId
        };
    }
}