using DndCompanion.Core.Domain.Entities;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DndCompanion.Tests;

/// <summary>
/// Integration tests for the SQLite/EF Core persistence layer. Each test uses
/// its own temporary campaign folder, mirroring the real
/// <c>Campagne/NomeCampagna/campaign.db</c> layout.
/// </summary>
public sealed class PersistenceTests
{
    private readonly CampaignDatabaseFactory _factory = new();
    private readonly CampaignDatabaseInitializer _initializer;

    public PersistenceTests()
    {
        _initializer = new CampaignDatabaseInitializer(
            _factory,
            NullLogger<CampaignDatabaseInitializer>.Instance);
    }

    private string CreateTempCampaignFolder()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "DndCompanionTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);
        return directory;
    }

    private async Task<CampaignDbContext> CreateContextAsync(string folder)
    {
        await _initializer.InitializeAsync(folder);
        return _factory.CreateContext(folder);
    }

    private static Campaign CreateCampaign(string name = "La Campagna di Test")
    {
        return new Campaign { Name = name };
    }

    [Fact]
    public async Task Initialize_CreatesDatabaseFile_WithSchema()
    {
        var folder = CreateTempCampaignFolder();

        await _initializer.InitializeAsync(folder);

        Assert.True(File.Exists(_factory.GetDatabaseFilePath(folder)));

        await using var connection = new SqliteConnection(
            $"Data Source={_factory.GetDatabaseFilePath(folder)}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN " +
                              "('Campaigns','Chapters','Scenes','Characters','Npcs','Locations','Quests'," +
                              "'Sessions','SessionEvents','DiceRolls','Combats','Combatants','InventoryItems','Notes')";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(14, count);
    }

    [Fact]
    public async Task InsertCampaign_And_Reload()
    {
        var folder = CreateTempCampaignFolder();
        var campaign = CreateCampaign();
        campaign.Description = "Una storia epica";

        await using (var context = await CreateContextAsync(folder))
        {
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();
        }

        await using var reloadedContext = _factory.CreateContext(folder);
        var reloaded = await reloadedContext.Campaigns.SingleAsync(c => c.Id == campaign.Id);

        Assert.Equal(campaign.Name, reloaded.Name);
        Assert.Equal(campaign.Description, reloaded.Description);
        Assert.Equal(campaign.CreatedAt, reloaded.CreatedAt);
    }

    [Fact]
    public async Task InsertCharacter_And_Reload_WithInventory()
    {
        var folder = CreateTempCampaignFolder();
        var campaign = CreateCampaign();

        var character = new Character
        {
            CampaignId = campaign.Id,
            Name = "Arkon",
            Class = "Paladino",
            Level = 5,
            MaxHp = 40,
            CurrentHp = 40,
            Conditions = CharacterCondition.Poisoned | CharacterCondition.Blinded
        };
        character.Inventory.Add(new InventoryItem { Name = "Spada Lunga", Quantity = 1, Weight = 3 });

        await using (var context = await CreateContextAsync(folder))
        {
            context.Campaigns.Add(campaign);
            context.Characters.Add(character);
            await context.SaveChangesAsync();
        }

        await using var reloadedContext = _factory.CreateContext(folder);
        var reloaded = await reloadedContext.Characters
            .Include(c => c.Inventory)
            .SingleAsync(c => c.Id == character.Id);

        Assert.Equal("Arkon", reloaded.Name);
        Assert.Equal(3, reloaded.ProficiencyBonus);
        Assert.Equal(CharacterCondition.Poisoned | CharacterCondition.Blinded, reloaded.Conditions);
        Assert.Single(reloaded.Inventory);
        Assert.Equal("Spada Lunga", reloaded.Inventory[0].Name);
    }

    [Fact]
    public async Task InsertScene_And_Reload()
    {
        var folder = CreateTempCampaignFolder();
        var campaign = CreateCampaign();

        var chapter = new Chapter { CampaignId = campaign.Id, Title = "Capitolo 1", Order = 1 };
        var scene = new Scene
        {
            ChapterId = chapter.Id,
            Title = "La Taverna",
            Order = 1,
            IsCompleted = true
        };
        chapter.Scenes.Add(scene);

        await using (var context = await CreateContextAsync(folder))
        {
            context.Campaigns.Add(campaign);
            context.Chapters.Add(chapter);
            await context.SaveChangesAsync();
        }

        await using var reloadedContext = _factory.CreateContext(folder);
        var reloaded = await reloadedContext.Scenes.SingleAsync(s => s.Id == scene.Id);

        Assert.Equal("La Taverna", reloaded.Title);
        Assert.Equal(chapter.Id, reloaded.ChapterId);
        Assert.True(reloaded.IsCompleted);
    }

    [Fact]
    public async Task Campaign_To_Scene_Relationship_IsPreserved()
    {
        var folder = CreateTempCampaignFolder();
        var campaign = CreateCampaign();

        var chapter = new Chapter { CampaignId = campaign.Id, Title = "Prologo", Order = 1 };
        chapter.Scenes.Add(new Scene { Title = "Scena 1", Order = 1 });
        chapter.Scenes.Add(new Scene { Title = "Scena 2", Order = 2 });
        campaign.Chapters.Add(chapter);

        await using (var context = await CreateContextAsync(folder))
        {
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();
        }

        await using var reloadedContext = _factory.CreateContext(folder);
        var reloaded = await reloadedContext.Campaigns
            .Include(c => c.Chapters)
                .ThenInclude(ch => ch.Scenes)
            .SingleAsync(c => c.Id == campaign.Id);

        var reloadedChapter = Assert.Single(reloaded.Chapters);
        Assert.Equal(2, reloadedChapter.Scenes.Count);
        Assert.Equal(chapter.Id, reloadedChapter.Scenes[0].ChapterId);
        Assert.All(reloadedChapter.Scenes, s => Assert.Equal(chapter.Id, s.ChapterId));
    }

    [Fact]
    public async Task Campaign_To_Character_Relationship_IsPreserved()
    {
        var folder = CreateTempCampaignFolder();
        var campaign = CreateCampaign();

        campaign.Characters.Add(new Character { Name = "Aria", Class = "Maga" });
        campaign.Characters.Add(new Character { Name = "Bram", Class = "Barbaro" });

        await using (var context = await CreateContextAsync(folder))
        {
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();
        }

        await using var reloadedContext = _factory.CreateContext(folder);
        var reloaded = await reloadedContext.Campaigns
            .Include(c => c.Characters)
            .SingleAsync(c => c.Id == campaign.Id);

        Assert.Equal(2, reloaded.Characters.Count);
        Assert.All(reloaded.Characters, ch => Assert.Equal(campaign.Id, ch.CampaignId));
    }

    [Fact]
    public async Task FullGraph_PersistsAndReloads()
    {
        var folder = CreateTempCampaignFolder();
        var campaign = CreateCampaign();

        var npc = new Npc { CampaignId = campaign.Id, Name = "Goblin", MaxHp = 7, CurrentHp = 7 };
        var quest = new Quest { CampaignId = campaign.Id, Title = "Salva il villaggio", Status = QuestStatus.Active };
        var location = new Location { CampaignId = campaign.Id, Name = "Villaggio di Harken" };
        var note = new Note { CampaignId = campaign.Id, Title = "Nota", Content = "Il goblin teme il fuoco", IsPinned = true };

        var session = new Session { CampaignId = campaign.Id, Number = 1, Title = "Sessione 1" };
        session.Start();

        session.Events.Add(new SessionEvent
        {
            Type = SessionEventType.NpcDefeated,
            Description = "Il Goblin viene sconfitto",
            RelatedNpcId = npc.Id
        });

        session.DiceRolls.Add(new DiceRoll
        {
            DiceNotation = "1d20 + 4",
            DiceType = DiceType.D20,
            Modifier = 4,
            Results = new List<int> { 13 },
            Purpose = "Attacco"
        });

        var combat = new Combat { SessionId = session.Id };
        combat.AddCombatant(new Combatant
        {
            CombatId = combat.Id,
            NpcId = npc.Id,
            Type = CombatantType.Npc,
            Name = npc.Name,
            Initiative = 10,
            MaxHp = 7,
            CurrentHp = 3,
            ArmorClass = 15
        });
        session.Combats.Add(combat);

        session.SessionNotes.Add(note);

        campaign.Npcs.Add(npc);
        campaign.Quests.Add(quest);
        campaign.Locations.Add(location);
        campaign.Notes.Add(note);
        campaign.Sessions.Add(session);

        await using (var context = await CreateContextAsync(folder))
        {
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();
        }

        await using var reloadedContext = _factory.CreateContext(folder);
        var reloaded = await reloadedContext.Campaigns
            .Include(c => c.Sessions)
                .ThenInclude(s => s.Events)
            .Include(c => c.Sessions)
                .ThenInclude(s => s.DiceRolls)
            .Include(c => c.Sessions)
                .ThenInclude(s => s.Combats)
                    .ThenInclude(cm => cm.Combatants)
            .Include(c => c.Npcs)
            .Include(c => c.Quests)
            .Include(c => c.Locations)
            .Include(c => c.Notes)
            .SingleAsync(c => c.Id == campaign.Id);

        Assert.Single(reloaded.Npcs);
        Assert.Single(reloaded.Quests);
        Assert.Single(reloaded.Locations);

        var reloadedSession = Assert.Single(reloaded.Sessions);
        Assert.True(reloadedSession.IsActive);

        var reloadedEvent = Assert.Single(reloadedSession.Events);
        Assert.Equal(SessionEventType.NpcDefeated, reloadedEvent.Type);
        Assert.Equal(npc.Id, reloadedEvent.RelatedNpcId);

        var reloadedRoll = Assert.Single(reloadedSession.DiceRolls);
        Assert.Equal(17, reloadedRoll.Total);

        var reloadedCombat = Assert.Single(reloadedSession.Combats);
        var reloadedCombatant = Assert.Single(reloadedCombat.Combatants);
        Assert.Equal(npc.Id, reloadedCombatant.NpcId);
        Assert.Equal("Goblin", reloadedCombatant.Name);

        var reloadedNote = Assert.Single(reloaded.Notes);
        Assert.True(reloadedNote.IsPinned);
    }

    [Fact]
    public async Task DeleteCampaign_Cascades_ToChildren()
    {
        var folder = CreateTempCampaignFolder();
        var campaign = CreateCampaign();
        var character = new Character { CampaignId = campaign.Id, Name = "Arkon" };
        var chapter = new Chapter { CampaignId = campaign.Id, Title = "Capitolo 1", Order = 1 };
        chapter.Scenes.Add(new Scene { Title = "Scena 1", Order = 1 });

        await using (var context = await CreateContextAsync(folder))
        {
            context.Campaigns.Add(campaign);
            context.Characters.Add(character);
            context.Chapters.Add(chapter);
            await context.SaveChangesAsync();
        }

        await using (var deleteContext = _factory.CreateContext(folder))
        {
            var loaded = await deleteContext.Campaigns.SingleAsync(c => c.Id == campaign.Id);
            deleteContext.Campaigns.Remove(loaded);
            await deleteContext.SaveChangesAsync();
        }

        await using var verifyContext = _factory.CreateContext(folder);
        Assert.Equal(0, await verifyContext.Characters.CountAsync());
        Assert.Equal(0, await verifyContext.Chapters.CountAsync());
        Assert.Equal(0, await verifyContext.Scenes.CountAsync());
    }

    [Fact]
    public async Task DeleteCharacter_DoesNotLose_CombatHistory()
    {
        var folder = CreateTempCampaignFolder();
        var campaign = CreateCampaign();
        var character = new Character { CampaignId = campaign.Id, Name = "Arkon" };
        var session = new Session { CampaignId = campaign.Id, Number = 1, Title = "S1" };

        var combat = new Combat { SessionId = session.Id };
        combat.AddCombatant(new Combatant
        {
            CombatId = combat.Id,
            CharacterId = character.Id,
            Type = CombatantType.Character,
            Name = character.Name,
            Initiative = 20,
            MaxHp = 30,
            CurrentHp = 12,
            ArmorClass = 18
        });
        session.Combats.Add(combat);

        await using (var context = await CreateContextAsync(folder))
        {
            context.Campaigns.Add(campaign);
            context.Characters.Add(character);
            context.Sessions.Add(session);
            await context.SaveChangesAsync();
        }

        await using (var deleteContext = _factory.CreateContext(folder))
        {
            var loaded = await deleteContext.Characters.SingleAsync(c => c.Id == character.Id);
            deleteContext.Characters.Remove(loaded);
            await deleteContext.SaveChangesAsync();
        }

        await using var verifyContext = _factory.CreateContext(folder);
        var combatant = await verifyContext.Combatants.SingleAsync();

        Assert.Null(combatant.CharacterId);
        Assert.Equal("Arkon", combatant.Name);
        Assert.Equal(12, combatant.CurrentHp);
    }

    [Fact]
    public async Task EachCampaign_HasItsOwnDatabase()
    {
        var folderA = CreateTempCampaignFolder();
        var folderB = CreateTempCampaignFolder();

        await using (var contextA = await CreateContextAsync(folderA))
        {
            contextA.Campaigns.Add(CreateCampaign("Campagna A"));
            await contextA.SaveChangesAsync();
        }

        await using (var contextB = await CreateContextAsync(folderB))
        {
            contextB.Campaigns.Add(CreateCampaign("Campagna B"));
            await contextB.SaveChangesAsync();
        }

        Assert.Equal(1, await _factory.CreateContext(folderA).Campaigns.CountAsync());
        Assert.Equal(1, await _factory.CreateContext(folderB).Campaigns.CountAsync());

        var campaignB = await _factory.CreateContext(folderB).Campaigns.SingleAsync();
        Assert.Equal("Campagna B", campaignB.Name);
    }
}