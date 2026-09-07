using DndCompanion.Core.Story;
using DndCompanion.Infrastructure.Data;
using DndCompanion.Infrastructure.Story;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DndCompanion.Tests;

/// <summary>
/// Tests for <see cref="SceneService"/>. Every test uses a fresh temporary
/// campaign folder and database, mirroring the real <c>Campagne</c> layout.
/// </summary>
public sealed class SceneServiceTests
{
    private readonly CampaignDatabaseFactory _factory = new();
    private readonly CampaignDatabaseInitializer _initializer;

    public SceneServiceTests()
    {
        _initializer = new CampaignDatabaseInitializer(
            _factory,
            NullLogger<CampaignDatabaseInitializer>.Instance);
    }

    private async Task<string> CreateCampaignFolderAsync()
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            "DndCompanionSceneTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(folder);
        await _initializer.InitializeAsync(folder);

        await using var context = _factory.CreateContext(folder);
        context.Campaigns.Add(new Core.Domain.Entities.Campaign { Name = Path.GetFileName(folder) });
        await context.SaveChangesAsync();

        return folder;
    }

    private async Task<Guid> CreateChapterAsync(string folder, string title = "Capitolo 1")
    {
        var service = new ChapterService(_factory, NullLogger<ChapterService>.Instance);
        var chapter = await service.CreateChapterAsync(folder, title, "");
        return chapter.Id;
    }

    private SceneService CreateService()
    {
        return new SceneService(_factory, NullLogger<SceneService>.Instance);
    }

    [Fact]
    public async Task Create_AppendsInOrder()
    {
        var folder = await CreateCampaignFolderAsync();
        var chapterId = await CreateChapterAsync(folder);
        var service = CreateService();

        var first = await service.CreateSceneAsync(folder, chapterId, "Scena 1", "");
        var second = await service.CreateSceneAsync(folder, chapterId, "Scena 2", "");

        Assert.Equal(1, first.Order);
        Assert.Equal(2, second.Order);
        Assert.Equal(chapterId, first.ChapterId);
    }

    [Fact]
    public async Task Create_MissingChapter_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<StoryException>(
            () => service.CreateSceneAsync(folder, Guid.NewGuid(), "Scena", ""));
    }

    [Fact]
    public async Task Update_ChangesTitleAndDescription()
    {
        var folder = await CreateCampaignFolderAsync();
        var chapterId = await CreateChapterAsync(folder);
        var service = CreateService();
        var scene = await service.CreateSceneAsync(folder, chapterId, "Vecchio", "Vecchia desc");

        var updated = await service.UpdateSceneAsync(folder, scene.Id, "Nuovo", "Nuova desc");

        Assert.Equal("Nuovo", updated.Title);
        Assert.Equal("Nuova desc", updated.Description);
    }

    [Fact]
    public async Task Update_MissingScene_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<StoryException>(
            () => service.UpdateSceneAsync(folder, Guid.NewGuid(), "X", ""));
    }

    [Fact]
    public async Task Delete_RemovesScene_AndRenumbers()
    {
        var folder = await CreateCampaignFolderAsync();
        var chapterId = await CreateChapterAsync(folder);
        var service = CreateService();

        var a = await service.CreateSceneAsync(folder, chapterId, "Scena A", "");
        var b = await service.CreateSceneAsync(folder, chapterId, "Scena B", "");
        var c = await service.CreateSceneAsync(folder, chapterId, "Scena C", "");

        await service.DeleteSceneAsync(folder, b.Id);

        var chapters = await new ChapterService(_factory, NullLogger<ChapterService>.Instance)
            .ListChaptersAsync(folder);

        var scenes = chapters.Single().Scenes;
        Assert.Equal(new[] { a.Id, c.Id }, scenes.Select(s => s.Id).ToArray());
        Assert.Equal(new[] { 1, 2 }, scenes.Select(s => s.Order).ToArray());
    }

    [Fact]
    public async Task Move_ReordersWithinChapter()
    {
        var folder = await CreateCampaignFolderAsync();
        var chapterId = await CreateChapterAsync(folder);
        var service = CreateService();

        var a = await service.CreateSceneAsync(folder, chapterId, "Scena A", "");
        var b = await service.CreateSceneAsync(folder, chapterId, "Scena B", "");
        var c = await service.CreateSceneAsync(folder, chapterId, "Scena C", "");

        await service.MoveSceneAsync(folder, c.Id, chapterId, 1);

        var chapters = await new ChapterService(_factory, NullLogger<ChapterService>.Instance)
            .ListChaptersAsync(folder);
        var scenes = chapters.Single().Scenes;

        Assert.Equal(new[] { c.Id, a.Id, b.Id }, scenes.Select(s => s.Id).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, scenes.Select(s => s.Order).ToArray());
    }

    [Fact]
    public async Task Move_AcrossChapters_MovesAndRenumbersBoth()
    {
        var folder = await CreateCampaignFolderAsync();
        var chapterA = await CreateChapterAsync(folder, "Capitolo A");
        var chapterB = await CreateChapterAsync(folder, "Capitolo B");
        var service = CreateService();

        var a1 = await service.CreateSceneAsync(folder, chapterA, "A1", "");
        var a2 = await service.CreateSceneAsync(folder, chapterA, "A2", "");
        var b1 = await service.CreateSceneAsync(folder, chapterB, "B1", "");

        await service.MoveSceneAsync(folder, a2.Id, chapterB, 2);

        var chapters = await new ChapterService(_factory, NullLogger<ChapterService>.Instance)
            .ListChaptersAsync(folder);

        var scenesA = chapters.Single(c => c.Id == chapterA).Scenes;
        var scenesB = chapters.Single(c => c.Id == chapterB).Scenes;

        Assert.Equal(new[] { a1.Id }, scenesA.Select(s => s.Id).ToArray());
        Assert.Equal(new[] { 1 }, scenesA.Select(s => s.Order).ToArray());
        Assert.Equal(new[] { b1.Id, a2.Id }, scenesB.Select(s => s.Id).ToArray());
        Assert.Equal(new[] { 1, 2 }, scenesB.Select(s => s.Order).ToArray());
    }

    [Fact]
    public async Task Move_MissingScene_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var chapterId = await CreateChapterAsync(folder);
        var service = CreateService();

        await Assert.ThrowsAsync<StoryException>(
            () => service.MoveSceneAsync(folder, Guid.NewGuid(), chapterId, 1));
    }

    [Fact]
    public async Task Complete_MarksSceneCompleted()
    {
        var folder = await CreateCampaignFolderAsync();
        var chapterId = await CreateChapterAsync(folder);
        var service = CreateService();
        var scene = await service.CreateSceneAsync(folder, chapterId, "Scena 1", "");

        var completed = await service.CompleteSceneAsync(folder, scene.Id);

        Assert.True(completed.IsCompleted);
    }

    [Fact]
    public async Task Complete_MissingScene_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<StoryException>(
            () => service.CompleteSceneAsync(folder, Guid.NewGuid()));
    }

    [Fact]
    public async Task SetCurrentScene_IsPersisted_OnCampaign()
    {
        var folder = await CreateCampaignFolderAsync();
        var chapterId = await CreateChapterAsync(folder);
        var service = CreateService();
        var scene = await service.CreateSceneAsync(folder, chapterId, "Scena 1", "");

        await service.SetCurrentSceneAsync(folder, scene.Id);

        await using var context = _factory.CreateContext(folder);
        var campaign = await context.Campaigns.SingleAsync();
        Assert.Equal(scene.Id, campaign.CurrentSceneId);
    }

    [Fact]
    public async Task SetCurrentScene_IsReflectedInList()
    {
        var folder = await CreateCampaignFolderAsync();
        var chapterId = await CreateChapterAsync(folder);
        var service = CreateService();
        var scene = await service.CreateSceneAsync(folder, chapterId, "Scena 1", "");

        await service.SetCurrentSceneAsync(folder, scene.Id);

        var chapters = await new ChapterService(_factory, NullLogger<ChapterService>.Instance)
            .ListChaptersAsync(folder);
        Assert.Equal(scene.Id, chapters.Single().CurrentSceneId);
    }

    [Fact]
    public async Task SetCurrentScene_MissingScene_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<StoryException>(
            () => service.SetCurrentSceneAsync(folder, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteCurrentScene_ClearsCurrentScene()
    {
        var folder = await CreateCampaignFolderAsync();
        var chapterId = await CreateChapterAsync(folder);
        var service = CreateService();
        var scene = await service.CreateSceneAsync(folder, chapterId, "Scena 1", "");

        await service.SetCurrentSceneAsync(folder, scene.Id);
        await service.DeleteSceneAsync(folder, scene.Id);

        await using var context = _factory.CreateContext(folder);
        var campaign = await context.Campaigns.SingleAsync();
        Assert.Null(campaign.CurrentSceneId);
    }
}