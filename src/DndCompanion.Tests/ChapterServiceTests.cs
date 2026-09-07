using DndCompanion.Core.Story;
using DndCompanion.Infrastructure.Data;
using DndCompanion.Infrastructure.Story;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DndCompanion.Tests;

/// <summary>
/// Tests for <see cref="ChapterService"/>. Every test uses a fresh temporary
/// campaign folder and database, mirroring the real <c>Campagne</c> layout.
/// </summary>
public sealed class ChapterServiceTests
{
    private readonly CampaignDatabaseFactory _factory = new();
    private readonly CampaignDatabaseInitializer _initializer;

    public ChapterServiceTests()
    {
        _initializer = new CampaignDatabaseInitializer(
            _factory,
            NullLogger<CampaignDatabaseInitializer>.Instance);
    }

    private async Task<string> CreateCampaignFolderAsync()
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            "DndCompanionChapterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(folder);
        await _initializer.InitializeAsync(folder);

        await using var context = _factory.CreateContext(folder);
        context.Campaigns.Add(new Core.Domain.Entities.Campaign { Name = Path.GetFileName(folder) });
        await context.SaveChangesAsync();

        return folder;
    }

    private ChapterService CreateService()
    {
        return new ChapterService(_factory, NullLogger<ChapterService>.Instance);
    }

    [Fact]
    public async Task Create_AppendsInOrder()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var first = await service.CreateChapterAsync(folder, "Capitolo 1", "Intro");
        var second = await service.CreateChapterAsync(folder, "Capitolo 2", "Sviluppo");

        Assert.Equal(1, first.Order);
        Assert.Equal(2, second.Order);
    }

    [Fact]
    public async Task List_ReturnsChaptersInOrder_WithScenes()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var chapter = await service.CreateChapterAsync(folder, "Capitolo 1", "");
        await CreateScenesAsync(folder, chapter.Id);

        var chapters = await service.ListChaptersAsync(folder);

        var listed = Assert.Single(chapters);
        Assert.Equal("Capitolo 1", listed.Title);
        Assert.Equal(2, listed.Scenes.Count);
        Assert.Equal("Scena 1", listed.Scenes[0].Title);
        Assert.Equal("Scena 2", listed.Scenes[1].Title);
    }

    [Fact]
    public async Task Update_ChangesTitleAndDescription()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var chapter = await service.CreateChapterAsync(folder, "Vecchio Titolo", "Vecchia descrizione");

        var updated = await service.UpdateChapterAsync(folder, chapter.Id, "Nuovo Titolo", "Nuova descrizione");

        Assert.Equal("Nuovo Titolo", updated.Title);
        Assert.Equal("Nuova descrizione", updated.Description);
    }

    [Fact]
    public async Task Update_MissingChapter_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<StoryException>(
            () => service.UpdateChapterAsync(folder, Guid.NewGuid(), "X", ""));
    }

    [Fact]
    public async Task Delete_RemovesChapter_AndRenumbersOthers()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var a = await service.CreateChapterAsync(folder, "Capitolo A", "");
        var b = await service.CreateChapterAsync(folder, "Capitolo B", "");
        var c = await service.CreateChapterAsync(folder, "Capitolo C", "");

        await service.DeleteChapterAsync(folder, b.Id);

        var remaining = await service.ListChaptersAsync(folder);
        Assert.Equal(2, remaining.Count);
        Assert.Equal(new[] { a.Id, c.Id }, remaining.Select(ch => ch.Id).ToArray());
        Assert.Equal(new[] { 1, 2 }, remaining.Select(ch => ch.Order).ToArray());
    }

    [Fact]
    public async Task Delete_WithScenes_Cascades()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var sceneService = new SceneService(_factory, NullLogger<SceneService>.Instance);

        var chapter = await service.CreateChapterAsync(folder, "Capitolo 1", "");
        var scene = await sceneService.CreateSceneAsync(folder, chapter.Id, "Scena 1", "");

        await service.DeleteChapterAsync(folder, chapter.Id);

        await using var context = _factory.CreateContext(folder);
        Assert.DoesNotContain(await context.Scenes.ToListAsync(), s => s.Id == scene.Id);
    }

    [Fact]
    public async Task Move_ReordersChapters()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var a = await service.CreateChapterAsync(folder, "Capitolo A", "");
        var b = await service.CreateChapterAsync(folder, "Capitolo B", "");
        var c = await service.CreateChapterAsync(folder, "Capitolo C", "");

        await service.MoveChapterAsync(folder, c.Id, 1);

        var chapters = await service.ListChaptersAsync(folder);
        Assert.Equal(new[] { c.Id, a.Id, b.Id }, chapters.Select(ch => ch.Id).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, chapters.Select(ch => ch.Order).ToArray());
    }

    [Fact]
    public async Task Move_MissingChapter_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<StoryException>(() => service.MoveChapterAsync(folder, Guid.NewGuid(), 1));
    }

    [Fact]
    public async Task Create_EmptyTitle_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<StoryException>(() => service.CreateChapterAsync(folder, "  ", ""));
    }

    private async Task CreateScenesAsync(string folder, Guid chapterId)
    {
        var sceneService = new SceneService(_factory, NullLogger<SceneService>.Instance);
        await sceneService.CreateSceneAsync(folder, chapterId, "Scena 1", "");
        await sceneService.CreateSceneAsync(folder, chapterId, "Scena 2", "");
    }
}