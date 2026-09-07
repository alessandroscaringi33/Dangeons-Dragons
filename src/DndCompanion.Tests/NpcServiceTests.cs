using DndCompanion.Core.Npcs;
using DndCompanion.Infrastructure.Data;
using DndCompanion.Infrastructure.Locations;
using DndCompanion.Infrastructure.Npcs;
using DndCompanion.Infrastructure.Story;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DndCompanion.Tests;

/// <summary>
/// Tests for <see cref="NpcService"/>. Every test uses a fresh temporary
/// campaign folder and database, mirroring the real <c>Campagne</c> layout.
/// </summary>
public sealed class NpcServiceTests
{
    private readonly CampaignDatabaseFactory _factory = new();
    private readonly CampaignDatabaseInitializer _initializer;

    public NpcServiceTests()
    {
        _initializer = new CampaignDatabaseInitializer(
            _factory,
            NullLogger<CampaignDatabaseInitializer>.Instance);
    }

    private async Task<string> CreateCampaignFolderAsync()
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            "DndCompanionNpcTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(folder);
        await _initializer.InitializeAsync(folder);

        await using var context = _factory.CreateContext(folder);
        context.Campaigns.Add(new Core.Domain.Entities.Campaign { Name = Path.GetFileName(folder) });
        await context.SaveChangesAsync();

        return folder;
    }

    private NpcService CreateService()
    {
        return new NpcService(_factory, NullLogger<NpcService>.Instance);
    }

    private LocationService CreateLocationService()
    {
        return new LocationService(_factory, NullLogger<LocationService>.Instance);
    }

    private static NpcDraft Draft(
        string name = "Goblin",
        string role = "Sgherro",
        int maxHp = 7,
        int armorClass = 15,
        int initiative = 2)
    {
        return new NpcDraft
        {
            Name = name,
            Role = role,
            Description = "Un piccolo umanoide verde.",
            MaxHp = maxHp,
            CurrentHp = maxHp,
            ArmorClass = armorClass,
            InitiativeModifier = initiative
        };
    }

    // ---------- CRUD ----------

    [Fact]
    public async Task Create_PersistsAllFields()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var created = await service.CreateNpcAsync(folder, Draft("Elminster", "Mago", 62, 13, 4));

        Assert.Equal("Elminster", created.Name);
        Assert.Equal("Mago", created.Role);
        Assert.Equal("Un piccolo umanoide verde.", created.Description);
        Assert.Equal(62, created.MaxHp);
        Assert.Equal(62, created.CurrentHp);
        Assert.Equal(13, created.ArmorClass);
        Assert.Equal(4, created.InitiativeModifier);
        Assert.True(created.IsAlive);
        Assert.False(created.IsKnown);
    }

    [Fact]
    public async Task List_ReturnsSortedByName()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.CreateNpcAsync(folder, Draft("Zara"));
        await service.CreateNpcAsync(folder, Draft("Arkon"));

        var npcs = await service.ListNpcsAsync(folder);

        Assert.Equal(new[] { "Arkon", "Zara" }, npcs.Select(n => n.Name).ToArray());
    }

    [Fact]
    public async Task Get_ReturnsNpc()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateNpcAsync(folder, Draft());

        var loaded = await service.GetNpcAsync(folder, created.Id);

        Assert.Equal(created.Id, loaded.Id);
        Assert.Equal("Goblin", loaded.Name);
    }

    [Fact]
    public async Task Get_MissingNpc_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<NpcException>(() => service.GetNpcAsync(folder, Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ChangesProfile()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateNpcAsync(folder, Draft());

        var updated = await service.UpdateNpcAsync(folder, created.Id, Draft(name: "Boss Orco", role: "Capo", maxHp: 60));

        Assert.Equal("Boss Orco", updated.Name);
        Assert.Equal("Capo", updated.Role);
        Assert.Equal(60, updated.MaxHp);
    }

    [Fact]
    public async Task Update_MissingNpc_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<NpcException>(() => service.UpdateNpcAsync(folder, Guid.NewGuid(), Draft()));
    }

    [Fact]
    public async Task Delete_RemovesNpc()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateNpcAsync(folder, Draft());

        await service.DeleteNpcAsync(folder, created.Id);

        var remaining = await service.ListNpcsAsync(folder);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Create_EmptyName_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<NpcException>(() => service.CreateNpcAsync(folder, Draft(name: "  ")));
    }

    // ---------- Search ----------

    [Fact]
    public async Task Search_FiltersByName_IgnoreCase()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.CreateNpcAsync(folder, Draft("Elminster", "Mago"));
        await service.CreateNpcAsync(folder, Draft("Goblin", "Sgherro"));
        await service.CreateNpcAsync(folder, Draft("ELMIRA", "Ranger"));

        var matches = await service.ListNpcsAsync(folder, "elm");

        Assert.Equal(new[] { "ELMIRA", "Elminster" }, matches.Select(n => n.Name).ToArray());
    }

    [Fact]
    public async Task Search_FiltersByRole()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.CreateNpcAsync(folder, Draft("A", "Mago"));
        await service.CreateNpcAsync(folder, Draft("B", "Sgherro"));

        var matches = await service.ListNpcsAsync(folder, "mago");

        Assert.Equal("A", Assert.Single(matches).Name);
    }

    [Fact]
    public async Task Search_WithNoMatches_ReturnsEmpty()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.CreateNpcAsync(folder, Draft());

        var matches = await service.ListNpcsAsync(folder, "inesistente");

        Assert.Empty(matches);
    }

    [Fact]
    public async Task Search_EmptyTerm_ReturnsAll()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.CreateNpcAsync(folder, Draft("A"));
        await service.CreateNpcAsync(folder, Draft("B"));

        var matches = await service.ListNpcsAsync(folder, "   ");

        Assert.Equal(2, matches.Count);
    }

    // ---------- HP / alive / dead ----------

    [Fact]
    public async Task ApplyDamage_ReducesHp_AndMarksDead_AtZero()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateNpcAsync(folder, Draft(maxHp: 10));

        var after = await service.ApplyDamageAsync(folder, created.Id, 10);

        Assert.Equal(0, after.CurrentHp);
        Assert.False(after.IsAlive);
    }

    [Fact]
    public async Task ApplyDamage_ClampsAtZero()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateNpcAsync(folder, Draft(maxHp: 10));

        var after = await service.ApplyDamageAsync(folder, created.Id, 100);

        Assert.Equal(0, after.CurrentHp);
        Assert.False(after.IsAlive);
    }

    [Fact]
    public async Task Heal_RevivesNpc()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateNpcAsync(folder, Draft(maxHp: 10));
        await service.ApplyDamageAsync(folder, created.Id, 10);

        var after = await service.HealAsync(folder, created.Id, 5);

        Assert.Equal(5, after.CurrentHp);
        Assert.True(after.IsAlive);
    }

    [Fact]
    public async Task Heal_NeverExceedsMax()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateNpcAsync(folder, Draft(maxHp: 10));
        await service.ApplyDamageAsync(folder, created.Id, 5);

        var after = await service.HealAsync(folder, created.Id, 100);

        Assert.Equal(10, after.CurrentHp);
    }

    [Fact]
    public async Task SetCurrentHp_ClampsToMax()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateNpcAsync(folder, Draft(maxHp: 10));

        var after = await service.SetCurrentHpAsync(folder, created.Id, 999);

        Assert.Equal(10, after.CurrentHp);
    }

    [Fact]
    public async Task SetAlive_MarksDead_Explicitly()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateNpcAsync(folder, Draft(maxHp: 10));

        var after = await service.SetAliveAsync(folder, created.Id, false);

        Assert.False(after.IsAlive);
        Assert.Equal(10, after.CurrentHp);
    }

    [Fact]
    public async Task SetAlive_Revives_AndGrantsOneHp()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateNpcAsync(folder, Draft(maxHp: 10));
        await service.ApplyDamageAsync(folder, created.Id, 10);
        await service.SetAliveAsync(folder, created.Id, false);

        var after = await service.SetAliveAsync(folder, created.Id, true);

        Assert.True(after.IsAlive);
        Assert.Equal(1, after.CurrentHp);
    }

    [Fact]
    public async Task HpAndAlive_PersistAcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateNpcAsync(folder, Draft(maxHp: 10));
        await service.ApplyDamageAsync(folder, created.Id, 7);

        var loaded = await service.GetNpcAsync(folder, created.Id);

        Assert.Equal(3, loaded.CurrentHp);
        Assert.True(loaded.IsAlive);
    }

    // ---------- Link to scene / location ----------

    [Fact]
    public async Task LinkToScene_LinksNpc()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var sceneService = new ChapterService(_factory, NullLogger<ChapterService>.Instance);
        var chapter = await sceneService.CreateChapterAsync(folder, "Capitolo 1", "");
        var scene = await new SceneService(_factory, NullLogger<SceneService>.Instance)
            .CreateSceneAsync(folder, chapter.Id, "La Taverna", "");

        var npc = await service.CreateNpcAsync(folder, Draft());
        var linked = await service.LinkToSceneAsync(folder, npc.Id, scene.Id);

        Assert.Equal(scene.Id, linked.SceneId);
        Assert.Equal("La Taverna", linked.SceneTitle);
    }

    [Fact]
    public async Task LinkToScene_ClearLink()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var sceneService = new ChapterService(_factory, NullLogger<ChapterService>.Instance);
        var chapter = await sceneService.CreateChapterAsync(folder, "C1", "");
        var scene = await new SceneService(_factory, NullLogger<SceneService>.Instance)
            .CreateSceneAsync(folder, chapter.Id, "S1", "");

        var npc = await service.CreateNpcAsync(folder, Draft());
        await service.LinkToSceneAsync(folder, npc.Id, scene.Id);

        var cleared = await service.LinkToSceneAsync(folder, npc.Id, null);

        Assert.Null(cleared.SceneId);
        Assert.Null(cleared.SceneTitle);
    }

    [Fact]
    public async Task LinkToScene_MissingScene_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var npc = await service.CreateNpcAsync(folder, Draft());

        await Assert.ThrowsAsync<NpcException>(() => service.LinkToSceneAsync(folder, npc.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task LinkToLocation_LinksNpc()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var locationService = CreateLocationService();
        var location = await locationService.CreateLocationAsync(folder, "Taverna del Drago", "");

        var npc = await service.CreateNpcAsync(folder, Draft());
        var linked = await service.LinkToLocationAsync(folder, npc.Id, location.Id);

        Assert.Equal(location.Id, linked.LocationId);
        Assert.Equal("Taverna del Drago", linked.LocationName);
    }

    [Fact]
    public async Task LinkToLocation_ClearLink()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var locationService = CreateLocationService();
        var location = await locationService.CreateLocationAsync(folder, "Piazza", "");

        var npc = await service.CreateNpcAsync(folder, Draft());
        await service.LinkToLocationAsync(folder, npc.Id, location.Id);

        var cleared = await service.LinkToLocationAsync(folder, npc.Id, null);

        Assert.Null(cleared.LocationId);
        Assert.Null(cleared.LocationName);
    }

    [Fact]
    public async Task LinkToLocation_MissingLocation_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var npc = await service.CreateNpcAsync(folder, Draft());

        await Assert.ThrowsAsync<NpcException>(() => service.LinkToLocationAsync(folder, npc.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Links_PersistAcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var locationService = CreateLocationService();
        var location = await locationService.CreateLocationAsync(folder, "Foresta", "");
        var npc = await service.CreateNpcAsync(folder, Draft());
        await service.LinkToLocationAsync(folder, npc.Id, location.Id);

        var loaded = await service.GetNpcAsync(folder, npc.Id);

        Assert.Equal(location.Id, loaded.LocationId);
        Assert.Equal("Foresta", loaded.LocationName);
    }

    // ---------- Deletion cascades ----------

    [Fact]
    public async Task DeleteLocation_UnlinksNpc()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var locationService = CreateLocationService();
        var location = await locationService.CreateLocationAsync(folder, "Castello", "");
        var npc = await service.CreateNpcAsync(folder, Draft());
        await service.LinkToLocationAsync(folder, npc.Id, location.Id);

        await locationService.DeleteLocationAsync(folder, location.Id);

        var loaded = await service.GetNpcAsync(folder, npc.Id);
        Assert.Null(loaded.LocationId);
    }

    [Fact]
    public async Task DeleteScene_UnlinksNpc()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var sceneService = new ChapterService(_factory, NullLogger<ChapterService>.Instance);
        var chapter = await sceneService.CreateChapterAsync(folder, "C1", "");
        var scene = await new SceneService(_factory, NullLogger<SceneService>.Instance)
            .CreateSceneAsync(folder, chapter.Id, "S1", "");
        var npc = await service.CreateNpcAsync(folder, Draft());
        await service.LinkToSceneAsync(folder, npc.Id, scene.Id);

        await sceneService.DeleteChapterAsync(folder, chapter.Id);

        var loaded = await service.GetNpcAsync(folder, npc.Id);
        Assert.Null(loaded.SceneId);
    }

    // ---------- Full persistence round-trip ----------

    [Fact]
    public async Task FullNpc_PersistsAllFields_AcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var draft = Draft(name: "Volothamp", role: "Cronista", maxHp: 40, armorClass: 12, initiative: 3);
        draft.CurrentHp = 25;
        draft.Description = "Un uomo robusto con un taccuino.";
        draft.Notes = "Conosce molte storie.";
        draft.IsKnown = true;

        var created = await service.CreateNpcAsync(folder, draft);

        var loaded = await service.GetNpcAsync(folder, created.Id);

        Assert.Equal("Volothamp", loaded.Name);
        Assert.Equal("Cronista", loaded.Role);
        Assert.Equal("Un uomo robusto con un taccuino.", loaded.Description);
        Assert.Equal(40, loaded.MaxHp);
        Assert.Equal(25, loaded.CurrentHp);
        Assert.Equal(12, loaded.ArmorClass);
        Assert.Equal(3, loaded.InitiativeModifier);
        Assert.Equal("Conosce molte storie.", loaded.Notes);
        Assert.True(loaded.IsKnown);
        Assert.True(loaded.IsAlive);
    }
}