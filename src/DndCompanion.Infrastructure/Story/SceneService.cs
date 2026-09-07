using DndCompanion.Core.Domain.Entities;
using DndCompanion.Core.Story;
using DndCompanion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.Story;

/// <summary>
/// Manages the scenes of a campaign story using the campaign's SQLite
/// database. Ordering is 1-based and kept contiguous after every mutation.
/// </summary>
public sealed class SceneService : ISceneService
{
    private const int TemporaryOrderOffset = 1_000_000;

    private readonly ICampaignDatabaseFactory _databaseFactory;
    private readonly ILogger<SceneService> _logger;

    public SceneService(
        ICampaignDatabaseFactory databaseFactory,
        ILogger<SceneService> logger)
    {
        _databaseFactory = databaseFactory;
        _logger = logger;
    }

    public async Task<SceneInfo> CreateSceneAsync(
        string campaignFolderPath,
        Guid chapterId,
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = ValidateTitle(title);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var chapterExists = await context.Chapters
            .AnyAsync(c => c.Id == chapterId, cancellationToken)
            .ConfigureAwait(false);

        if (!chapterExists)
        {
            throw new StoryException("Il capitolo non è stato trovato.");
        }

        var nextOrder = (await context.Scenes
                .CountAsync(s => s.ChapterId == chapterId, cancellationToken)
                .ConfigureAwait(false)) + 1;

        var scene = new Scene
        {
            ChapterId = chapterId,
            Title = normalizedTitle,
            Description = description ?? string.Empty,
            Order = nextOrder
        };

        context.Scenes.Add(scene);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created scene '{Title}' (order {Order}) in chapter {Chapter}", scene.Title, scene.Order, chapterId);
        return ToInfo(scene);
    }

    public async Task<SceneInfo> UpdateSceneAsync(
        string campaignFolderPath,
        Guid sceneId,
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = ValidateTitle(title);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var scene = await context.Scenes
            .FirstOrDefaultAsync(s => s.Id == sceneId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new StoryException("La scena non è stata trovata.");

        scene.Title = normalizedTitle;
        scene.Description = description ?? string.Empty;

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated scene '{Title}' in campaign {Folder}", scene.Title, campaignFolderPath);
        return ToInfo(scene);
    }

    public async Task DeleteSceneAsync(
        string campaignFolderPath,
        Guid sceneId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var scene = await context.Scenes
            .FirstOrDefaultAsync(s => s.Id == sceneId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new StoryException("La scena non è stata trovata.");

        var chapterId = scene.ChapterId;

        await ClearCurrentSceneIfNeededAsync(context, sceneId, cancellationToken).ConfigureAwait(false);

        context.Scenes.Remove(scene);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        await RenumberScenesAsync(context, chapterId, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted scene '{Title}' from campaign {Folder}", scene.Title, campaignFolderPath);
    }

    public async Task<SceneInfo> MoveSceneAsync(
        string campaignFolderPath,
        Guid sceneId,
        Guid targetChapterId,
        int newOrder,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var scene = await context.Scenes
            .FirstOrDefaultAsync(s => s.Id == sceneId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new StoryException("La scena non è stata trovata.");

        var targetExists = await context.Chapters
            .AnyAsync(c => c.Id == targetChapterId, cancellationToken)
            .ConfigureAwait(false);

        if (!targetExists)
        {
            throw new StoryException("Il capitolo di destinazione non è stato trovato.");
        }

        var sourceChapterId = scene.ChapterId;

        if (sourceChapterId != targetChapterId)
        {
            // Relocate the scene to the target chapter with a temporary order
            // that cannot collide with the target chapter's existing orders.
            scene.ChapterId = targetChapterId;
            scene.Order += TemporaryOrderOffset;
            await SaveAsync(context, cancellationToken).ConfigureAwait(false);

            // Compact the source chapter after the scene left it.
            await RenumberScenesAsync(context, sourceChapterId, cancellationToken).ConfigureAwait(false);
        }

        var targetScenes = await ScenesOfAsync(context, targetChapterId, cancellationToken).ConfigureAwait(false);
        targetScenes.Remove(scene);

        var targetIndex = Math.Clamp(newOrder - 1, 0, targetScenes.Count);
        targetScenes.Insert(targetIndex, scene);

        await AssignContiguousOrdersAsync(context, targetScenes, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Moved scene '{Title}' to chapter {Chapter} position {Order} in campaign {Folder}",
            scene.Title,
            targetChapterId,
            scene.Order,
            campaignFolderPath);

        return ToInfo(scene);
    }

    public async Task<SceneInfo> CompleteSceneAsync(
        string campaignFolderPath,
        Guid sceneId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var scene = await context.Scenes
            .FirstOrDefaultAsync(s => s.Id == sceneId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new StoryException("La scena non è stata trovata.");

        scene.Complete();
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Completed scene '{Title}' in campaign {Folder}", scene.Title, campaignFolderPath);
        return ToInfo(scene);
    }

    public async Task<SceneInfo> SetCurrentSceneAsync(
        string campaignFolderPath,
        Guid sceneId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var scene = await context.Scenes
            .FirstOrDefaultAsync(s => s.Id == sceneId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new StoryException("La scena non è stata trovata.");

        var campaign = await context.Campaigns
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new StoryException("La campagna non è stata trovata.");

        campaign.CurrentSceneId = scene.Id;
        campaign.Touch();
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Set current scene '{Title}' in campaign {Folder}", scene.Title, campaignFolderPath);
        return ToInfo(scene);
    }

    private static async Task<List<Scene>> ScenesOfAsync(
        CampaignDbContext context,
        Guid chapterId,
        CancellationToken cancellationToken)
    {
        return await context.Scenes
            .Where(s => s.ChapterId == chapterId)
            .OrderBy(s => s.Order)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task RenumberScenesAsync(
        CampaignDbContext context,
        Guid chapterId,
        CancellationToken cancellationToken)
    {
        var scenes = await ScenesOfAsync(context, chapterId, cancellationToken).ConfigureAwait(false);
        await AssignContiguousOrdersAsync(context, scenes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Rewrites the <see cref="Scene.Order"/> of the given scenes to contiguous
    /// 1..N values. Because <c>(ChapterId, Order)</c> is unique, orders are first
    /// shifted to large non-conflicting temporary values and then each final
    /// value is written with its own <see cref="CampaignDbContext.SaveChangesAsync"/>
    /// so EF Core never has to order conflicting updates in a single batch.
    /// </summary>
    private static async Task AssignContiguousOrdersAsync(
        CampaignDbContext context,
        List<Scene> scenes,
        CancellationToken cancellationToken)
    {
        if (scenes.Count == 0)
        {
            return;
        }

        for (var i = 0; i < scenes.Count; i++)
        {
            scenes[i].Order += TemporaryOrderOffset;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < scenes.Count; i++)
        {
            scenes[i].Order = i + 1;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ClearCurrentSceneIfNeededAsync(
        CampaignDbContext context,
        Guid sceneId,
        CancellationToken cancellationToken)
    {
        var campaign = await context.Campaigns
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (campaign is not null && campaign.CurrentSceneId == sceneId)
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
            throw new StoryException("Non è stato possibile salvare la scena.", ex);
        }
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new StoryException("Il titolo della scena è obbligatorio.");
        }

        return title.Trim();
    }

    private static SceneInfo ToInfo(Scene scene)
    {
        return new SceneInfo
        {
            Id = scene.Id,
            ChapterId = scene.ChapterId,
            Title = scene.Title,
            Description = scene.Description,
            Order = scene.Order,
            IsCompleted = scene.IsCompleted,
            Notes = scene.Notes
        };
    }
}