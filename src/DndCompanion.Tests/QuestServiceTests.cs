using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.Npcs;
using DndCompanion.Core.Quests;
using DndCompanion.Infrastructure.Data;
using DndCompanion.Infrastructure.Locations;
using DndCompanion.Infrastructure.Npcs;
using DndCompanion.Infrastructure.Quests;
using DndCompanion.Infrastructure.Story;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DndCompanion.Tests;

/// <summary>
/// Tests for <see cref="QuestService"/>. Every test uses a fresh temporary
/// campaign folder and database, mirroring the real <c>Campagne</c> layout.
/// </summary>
public sealed class QuestServiceTests
{
    private readonly CampaignDatabaseFactory _factory = new();
    private readonly CampaignDatabaseInitializer _initializer;

    public QuestServiceTests()
    {
        _initializer = new CampaignDatabaseInitializer(
            _factory,
            NullLogger<CampaignDatabaseInitializer>.Instance);
    }

    private async Task<string> CreateCampaignFolderAsync()
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            "DndCompanionQuestTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(folder);
        await _initializer.InitializeAsync(folder);

        await using var context = _factory.CreateContext(folder);
        context.Campaigns.Add(new Core.Domain.Entities.Campaign { Name = Path.GetFileName(folder) });
        await context.SaveChangesAsync();

        return folder;
    }

    private QuestService CreateService()
    {
        return new QuestService(_factory, NullLogger<QuestService>.Instance);
    }

    private static QuestDraft Draft(
        string title = "Salva il villaggio",
        QuestStatus status = QuestStatus.NotStarted,
        string description = "Libera Harken dai goblin.")
    {
        return new QuestDraft
        {
            Title = title,
            Description = description,
            Status = status
        };
    }

    // ---------- CRUD ----------

    [Fact]
    public async Task Create_PersistsFields()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var created = await service.CreateQuestAsync(folder, Draft());

        Assert.Equal("Salva il villaggio", created.Title);
        Assert.Equal("Libera Harken dai goblin.", created.Description);
        Assert.Equal(QuestStatus.NotStarted, created.Status);
        Assert.Empty(created.Notes);
    }

    [Fact]
    public async Task List_ReturnsQuests()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.CreateQuestAsync(folder, Draft("Quest A"));
        await service.CreateQuestAsync(folder, Draft("Quest B", QuestStatus.Active));

        var quests = await service.ListQuestsAsync(folder);

        Assert.Equal(2, quests.Count);
    }

    [Fact]
    public async Task Get_ReturnsQuest()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateQuestAsync(folder, Draft());

        var loaded = await service.GetQuestAsync(folder, created.Id);

        Assert.Equal(created.Id, loaded.Id);
        Assert.Equal("Salva il villaggio", loaded.Title);
    }

    [Fact]
    public async Task Get_MissingQuest_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<QuestException>(() => service.GetQuestAsync(folder, Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateQuestAsync(folder, Draft());

        var draft = Draft("Nuovo obiettivo", QuestStatus.Active, "Nuova desc");
        draft.Notes = "Nota";
        var updated = await service.UpdateQuestAsync(folder, created.Id, draft);

        Assert.Equal("Nuovo obiettivo", updated.Title);
        Assert.Equal(QuestStatus.Active, updated.Status);
        Assert.Equal("Nuova desc", updated.Description);
        Assert.Equal("Nota", updated.Notes);
    }

    [Fact]
    public async Task Update_MissingQuest_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<QuestException>(() => service.UpdateQuestAsync(folder, Guid.NewGuid(), Draft()));
    }

    [Fact]
    public async Task Delete_RemovesQuest()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateQuestAsync(folder, Draft());

        await service.DeleteQuestAsync(folder, created.Id);

        Assert.Empty(await service.ListQuestsAsync(folder));
    }

    [Fact]
    public async Task Create_EmptyTitle_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<QuestException>(() => service.CreateQuestAsync(folder, Draft(title: "  ")));
    }

    // ---------- Search ----------

    [Fact]
    public async Task Search_FiltersByTitle_IgnoreCase()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.CreateQuestAsync(folder, Draft("Recupera il tesoro"));
        await service.CreateQuestAsync(folder, Draft("Caccia al drago"));

        var matches = await service.ListQuestsAsync(folder, "tesoro");

        Assert.Equal("Recupera il tesoro", Assert.Single(matches).Title);
    }

    [Fact]
    public async Task Search_FiltersByDescription()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.CreateQuestAsync(folder, Draft("A", description: "Il mago è scomparso"));
        await service.CreateQuestAsync(folder, Draft("B", description: "Il re è malato"));

        var matches = await service.ListQuestsAsync(folder, "mago");

        Assert.Equal("A", Assert.Single(matches).Title);
    }

    [Fact]
    public async Task Search_NoMatches_ReturnsEmpty()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.CreateQuestAsync(folder, Draft());

        Assert.Empty(await service.ListQuestsAsync(folder, "inesistente"));
    }

    // ---------- Status ----------

    [Fact]
    public async Task SetStatus_EachValue_IsPersisted()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateQuestAsync(folder, Draft());

        foreach (var status in new[]
                 {
                     QuestStatus.Active,
                     QuestStatus.Completed,
                     QuestStatus.Failed,
                     QuestStatus.Abandoned,
                     QuestStatus.NotStarted
                 })
        {
            var updated = await service.SetStatusAsync(folder, created.Id, status);
            Assert.Equal(status, updated.Status);
        }
    }

    [Fact]
    public async Task SetStatus_MissingQuest_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<QuestException>(
            () => service.SetStatusAsync(folder, Guid.NewGuid(), QuestStatus.Active));
    }

    [Fact]
    public async Task Status_PersistsAcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateQuestAsync(folder, Draft());
        await service.SetStatusAsync(folder, created.Id, QuestStatus.Completed);

        var loaded = await service.GetQuestAsync(folder, created.Id);

        Assert.Equal(QuestStatus.Completed, loaded.Status);
    }

    // ---------- Links ----------

    [Fact]
    public async Task LinkToChapter_LinksQuest()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var chapterService = new ChapterService(_factory, NullLogger<ChapterService>.Instance);
        var chapter = await chapterService.CreateChapterAsync(folder, "Capitolo 1", "");
        var quest = await service.CreateQuestAsync(folder, Draft());

        var linked = await service.LinkToChapterAsync(folder, quest.Id, chapter.Id);

        Assert.Equal(chapter.Id, linked.ChapterId);
        Assert.Equal("Capitolo 1", linked.ChapterTitle);
    }

    [Fact]
    public async Task LinkToScene_LinksQuest()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var chapterService = new ChapterService(_factory, NullLogger<ChapterService>.Instance);
        var sceneService = new SceneService(_factory, NullLogger<SceneService>.Instance);
        var chapter = await chapterService.CreateChapterAsync(folder, "C1", "");
        var scene = await sceneService.CreateSceneAsync(folder, chapter.Id, "Scena 1", "");
        var quest = await service.CreateQuestAsync(folder, Draft());

        var linked = await service.LinkToSceneAsync(folder, quest.Id, scene.Id);

        Assert.Equal(scene.Id, linked.SceneId);
        Assert.Equal("Scena 1", linked.SceneTitle);
    }

    [Fact]
    public async Task LinkToLocation_LinksQuest()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var locationService = new LocationService(_factory, NullLogger<LocationService>.Instance);
        var location = await locationService.CreateLocationAsync(folder, "Taverna", "");
        var quest = await service.CreateQuestAsync(folder, Draft());

        var linked = await service.LinkToLocationAsync(folder, quest.Id, location.Id);

        Assert.Equal(location.Id, linked.LocationId);
        Assert.Equal("Taverna", linked.LocationName);
    }

    [Fact]
    public async Task LinkToNpc_LinksQuest()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var npcService = new NpcService(_factory, NullLogger<NpcService>.Instance);
        var npc = await npcService.CreateNpcAsync(folder, new NpcDraft { Name = "Elminster", MaxHp = 40, CurrentHp = 40 });
        var quest = await service.CreateQuestAsync(folder, Draft());

        var linked = await service.LinkToNpcAsync(folder, quest.Id, npc.Id);

        Assert.Equal(npc.Id, linked.NpcId);
        Assert.Equal("Elminster", linked.NpcName);
    }

    [Fact]
    public async Task LinkToChapter_MissingChapter_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var quest = await service.CreateQuestAsync(folder, Draft());

        await Assert.ThrowsAsync<QuestException>(() => service.LinkToChapterAsync(folder, quest.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task LinkToScene_MissingScene_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var quest = await service.CreateQuestAsync(folder, Draft());

        await Assert.ThrowsAsync<QuestException>(() => service.LinkToSceneAsync(folder, quest.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task LinkToLocation_MissingLocation_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var quest = await service.CreateQuestAsync(folder, Draft());

        await Assert.ThrowsAsync<QuestException>(() => service.LinkToLocationAsync(folder, quest.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task LinkToNpc_MissingNpc_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var quest = await service.CreateQuestAsync(folder, Draft());

        await Assert.ThrowsAsync<QuestException>(() => service.LinkToNpcAsync(folder, quest.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Links_ClearWithNull()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var locationService = new LocationService(_factory, NullLogger<LocationService>.Instance);
        var location = await locationService.CreateLocationAsync(folder, "Foresta", "");
        var quest = await service.CreateQuestAsync(folder, Draft());
        await service.LinkToLocationAsync(folder, quest.Id, location.Id);

        var cleared = await service.LinkToLocationAsync(folder, quest.Id, null);

        Assert.Null(cleared.LocationId);
        Assert.Null(cleared.LocationName);
    }

    // ---------- Cascade unlink ----------

    [Fact]
    public async Task DeleteLocation_UnlinksQuest()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var locationService = new LocationService(_factory, NullLogger<LocationService>.Instance);
        var location = await locationService.CreateLocationAsync(folder, "Castello", "");
        var quest = await service.CreateQuestAsync(folder, Draft());
        await service.LinkToLocationAsync(folder, quest.Id, location.Id);

        await locationService.DeleteLocationAsync(folder, location.Id);

        var loaded = await service.GetQuestAsync(folder, quest.Id);
        Assert.Null(loaded.LocationId);
    }

    [Fact]
    public async Task DeleteChapter_UnlinksQuest()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var chapterService = new ChapterService(_factory, NullLogger<ChapterService>.Instance);
        var chapter = await chapterService.CreateChapterAsync(folder, "C1", "");
        var quest = await service.CreateQuestAsync(folder, Draft());
        await service.LinkToChapterAsync(folder, quest.Id, chapter.Id);

        await chapterService.DeleteChapterAsync(folder, chapter.Id);

        var loaded = await service.GetQuestAsync(folder, quest.Id);
        Assert.Null(loaded.ChapterId);
    }

    [Fact]
    public async Task DeleteNpc_UnlinksQuest()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var npcService = new NpcService(_factory, NullLogger<NpcService>.Instance);
        var npc = await npcService.CreateNpcAsync(folder, new NpcDraft { Name = "G", MaxHp = 5, CurrentHp = 5 });
        var quest = await service.CreateQuestAsync(folder, Draft());
        await service.LinkToNpcAsync(folder, quest.Id, npc.Id);

        await npcService.DeleteNpcAsync(folder, npc.Id);

        var loaded = await service.GetQuestAsync(folder, quest.Id);
        Assert.Null(loaded.NpcId);
    }

    // ---------- Full persistence ----------

    [Fact]
    public async Task FullQuest_PersistsAllLinks_AcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var chapterService = new ChapterService(_factory, NullLogger<ChapterService>.Instance);
        var sceneService = new SceneService(_factory, NullLogger<SceneService>.Instance);
        var locationService = new LocationService(_factory, NullLogger<LocationService>.Instance);
        var npcService = new NpcService(_factory, NullLogger<NpcService>.Instance);

        var chapter = await chapterService.CreateChapterAsync(folder, "Capitolo 1", "");
        var scene = await sceneService.CreateSceneAsync(folder, chapter.Id, "Scena 1", "");
        var location = await locationService.CreateLocationAsync(folder, "Dungeon", "");
        var npc = await npcService.CreateNpcAsync(folder, new NpcDraft { Name = "Goblin", MaxHp = 7, CurrentHp = 7 });

        var draft = Draft("Obiettivo", QuestStatus.Active, "Desc");
        draft.Notes = "Nota persistente";
        draft.ChapterId = chapter.Id;
        draft.SceneId = scene.Id;
        draft.LocationId = location.Id;
        draft.NpcId = npc.Id;

        var created = await service.CreateQuestAsync(folder, draft);

        var loaded = await service.GetQuestAsync(folder, created.Id);

        Assert.Equal("Obiettivo", loaded.Title);
        Assert.Equal(QuestStatus.Active, loaded.Status);
        Assert.Equal("Nota persistente", loaded.Notes);
        Assert.Equal(chapter.Id, loaded.ChapterId);
        Assert.Equal("Capitolo 1", loaded.ChapterTitle);
        Assert.Equal(scene.Id, loaded.SceneId);
        Assert.Equal("Scena 1", loaded.SceneTitle);
        Assert.Equal(location.Id, loaded.LocationId);
        Assert.Equal("Dungeon", loaded.LocationName);
        Assert.Equal(npc.Id, loaded.NpcId);
        Assert.Equal("Goblin", loaded.NpcName);
    }
}