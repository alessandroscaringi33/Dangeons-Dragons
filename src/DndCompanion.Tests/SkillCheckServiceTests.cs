using DndCompanion.Core.Dice;
using DndCompanion.Core.Domain.Entities;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.SkillChecks;
using DndCompanion.Infrastructure.Data;
using DndCompanion.Infrastructure.SkillChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DndCompanion.Tests;

/// <summary>
/// Tests for <see cref="SkillCheckService"/>. Every test uses a fresh temporary
/// campaign folder and database, mirroring the real <c>Campagne</c> layout.
/// </summary>
public sealed class SkillCheckServiceTests
{
    private readonly CampaignDatabaseFactory _factory = new();
    private readonly CampaignDatabaseInitializer _initializer;

    public SkillCheckServiceTests()
    {
        _initializer = new CampaignDatabaseInitializer(
            _factory,
            NullLogger<CampaignDatabaseInitializer>.Instance);
    }

    private async Task<string> CreateCampaignFolderAsync()
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            "DndCompanionSkillCheckTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(folder);
        await _initializer.InitializeAsync(folder);

        await using var context = _factory.CreateContext(folder);
        context.Campaigns.Add(new Campaign { Name = Path.GetFileName(folder) });
        await context.SaveChangesAsync();

        return folder;
    }

    private SkillCheckService CreateService()
    {
        return new SkillCheckService(
            _factory,
            new DiceService(),
            NullLogger<SkillCheckService>.Instance);
    }

    private static SkillCheckDraft Draft(
        string skill = "Percezione",
        int dc = 15,
        int modifier = 4,
        bool physical = false,
        int? physicalRoll = null)
    {
        return new SkillCheckDraft
        {
            Skill = skill,
            DiceNotation = "1d20",
            Modifier = modifier,
            DifficultyClass = dc,
            IsPhysicalRoll = physical,
            PhysicalRoll = physicalRoll
        };
    }

    // ---------- Registration / session ----------

    [Fact]
    public async Task RecordCheck_CreatesActiveSession_AndStoresCheck()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var check = await service.RecordCheckAsync(folder, Draft(physical: true, physicalRoll: 13));

        Assert.Equal("Percezione", check.Skill);
        Assert.Equal(13, check.Roll);
        Assert.Equal(4, check.Modifier);
        Assert.Equal(17, check.Total);
        Assert.True(check.IsSuccess);
        Assert.True(check.IsPhysicalRoll);

        await using var context = _factory.CreateContext(folder);
        var stored = await context.SkillChecks.SingleAsync();
        Assert.Equal(13, stored.Roll);
        Assert.Equal(17, stored.Total);
        Assert.True(stored.IsSuccess);
    }

    [Fact]
    public async Task RecordCheck_AddsEventToSessionTimeline()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.RecordCheckAsync(folder, Draft(physical: true, physicalRoll: 10));

        await using var context = _factory.CreateContext(folder);
        var session = await context.Sessions.SingleAsync();
        var evt = await context.SessionEvents.SingleAsync();

        Assert.True(session.IsActive);
        Assert.Equal(SessionEventType.SkillCheck, evt.Type);
        Assert.Contains("Percezione", evt.Description);
        Assert.Contains("CD 15", evt.Description);
    }

    [Fact]
    public async Task RecordCheck_ReusesActiveSession()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.RecordCheckAsync(folder, Draft(physical: true, physicalRoll: 10));
        await service.RecordCheckAsync(folder, Draft(skill: "Acrobazia", physical: true, physicalRoll: 5));

        await using var context = _factory.CreateContext(folder);
        Assert.Equal(1, await context.Sessions.CountAsync());
        Assert.Equal(2, await context.SkillChecks.CountAsync());
        Assert.Equal(2, await context.SessionEvents.CountAsync());
    }

    // ---------- Outcome ----------

    [Fact]
    public async Task RecordCheck_TotalEqualsCd_IsSuccess()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var check = await service.RecordCheckAsync(folder, Draft(dc: 17, physical: true, physicalRoll: 13));

        Assert.Equal(17, check.Total);
        Assert.True(check.IsSuccess);
        Assert.Equal("SUCCESSO", check.OutcomeText);
    }

    [Fact]
    public async Task RecordCheck_TotalBelowCd_IsFailure()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var check = await service.RecordCheckAsync(folder, Draft(dc: 18, physical: true, physicalRoll: 13));

        Assert.Equal(17, check.Total);
        Assert.False(check.IsSuccess);
        Assert.Equal("FALLIMENTO", check.OutcomeText);
    }

    // ---------- Physical roll ----------

    [Fact]
    public async Task PhysicalRoll_IsUsedVerbatim()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var check = await service.RecordCheckAsync(folder, Draft(physical: true, physicalRoll: 17));

        Assert.Equal(17, check.Roll);
        Assert.True(check.IsPhysicalRoll);
        Assert.Equal(21, check.Total); // 17 + 4
    }

    [Fact]
    public async Task PhysicalRoll_OutOfRange_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<SkillCheckException>(
            () => service.RecordCheckAsync(folder, Draft(physical: true, physicalRoll: 25)));
    }

    [Fact]
    public async Task PhysicalRoll_MissingValue_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<SkillCheckException>(
            () => service.RecordCheckAsync(folder, Draft(physical: true, physicalRoll: null)));
    }

    [Fact]
    public async Task GeneratedRoll_IsNotPhysical()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var check = await service.RecordCheckAsync(folder, Draft());

        Assert.False(check.IsPhysicalRoll);
        Assert.InRange(check.Roll, 1, 20);
    }

    [Fact]
    public async Task GeneratedRoll_TotalMatchesRollPlusModifier()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var check = await service.RecordCheckAsync(folder, Draft(modifier: 0));

        Assert.Equal(check.Roll, check.Total);
    }

    // ---------- Character link ----------

    [Fact]
    public async Task RecordCheck_WithCharacter_LinksName()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await using (var context = _factory.CreateContext(folder))
        {
            context.Characters.Add(new Character { CampaignId = (await context.Campaigns.FirstAsync()).Id, Name = "Arkon", MaxHp = 40 });
            await context.SaveChangesAsync();
        }

        var characterId = await GetFirstCharacterIdAsync(folder);

        var draft = Draft(physical: true, physicalRoll: 13);
        draft.CharacterId = characterId;
        var check = await service.RecordCheckAsync(folder, draft);

        Assert.Equal(characterId, check.CharacterId);
        Assert.Equal("Arkon", check.CharacterName);
    }

    [Fact]
    public async Task RecordCheck_WithoutCharacter_NoLink()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var check = await service.RecordCheckAsync(folder, Draft(physical: true, physicalRoll: 13));

        Assert.Null(check.CharacterId);
        Assert.Null(check.CharacterName);
    }

    // ---------- Validation ----------

    [Fact]
    public async Task RecordCheck_EmptySkill_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<SkillCheckException>(
            () => service.RecordCheckAsync(folder, Draft(skill: "  ", physical: true, physicalRoll: 10)));
    }

    [Fact]
    public async Task RecordCheck_NegativeDc_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<SkillCheckException>(
            () => service.RecordCheckAsync(folder, Draft(dc: -1, physical: true, physicalRoll: 10)));
    }

    [Fact]
    public async Task RecordCheck_InvalidDiceNotation_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var draft = Draft();
        draft.DiceNotation = "1d7";
        await Assert.ThrowsAsync<SkillCheckException>(
            () => service.RecordCheckAsync(folder, draft));
    }

    // ---------- List ----------

    [Fact]
    public async Task ListChecks_ReturnsMostRecentFirst()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.RecordCheckAsync(folder, Draft(skill: "A", physical: true, physicalRoll: 5));
        await service.RecordCheckAsync(folder, Draft(skill: "B", physical: true, physicalRoll: 10));

        var checks = await service.ListChecksAsync(folder);

        Assert.Equal(2, checks.Count);
        Assert.Equal("B", checks[0].Skill);
        Assert.Equal("A", checks[1].Skill);
    }

    [Fact]
    public async Task ListChecks_RespectsLimit()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        for (var i = 0; i < 5; i++)
        {
            await service.RecordCheckAsync(folder, Draft(skill: $"Skill{i}", physical: true, physicalRoll: 5));
        }

        var checks = await service.ListChecksAsync(folder, limit: 2);

        Assert.Equal(2, checks.Count);
    }

    // ---------- Persistence ----------

    [Fact]
    public async Task RecordedCheck_PersistsAcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var recorded = await service.RecordCheckAsync(folder, Draft(physical: true, physicalRoll: 13, modifier: 4));

        var checks = await service.ListChecksAsync(folder);

        var loaded = Assert.Single(checks);
        Assert.Equal(recorded.Id, loaded.Id);
        Assert.Equal("Percezione", loaded.Skill);
        Assert.Equal(17, loaded.Total);
        Assert.True(loaded.IsSuccess);
        Assert.True(loaded.IsPhysicalRoll);
    }

    [Fact]
    public async Task MultipleSessions_NumberIncrements()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.RecordCheckAsync(folder, Draft(physical: true, physicalRoll: 10));

        // End the active session so the next check starts a new one.
        await using (var context = _factory.CreateContext(folder))
        {
            var campaign = await context.Campaigns.Include(c => c.Sessions).FirstAsync();
            campaign.Sessions.Single().End();
            campaign.ActiveSessionId = null;
            await context.SaveChangesAsync();
        }

        await service.RecordCheckAsync(folder, Draft(physical: true, physicalRoll: 10));

        await using var verifyContext = _factory.CreateContext(folder);
        var numbers = await verifyContext.Sessions.Select(s => s.Number).ToListAsync();
        Assert.Equal(new[] { 1, 2 }, numbers.OrderBy(n => n).ToArray());
    }

    private async Task<Guid> GetFirstCharacterIdAsync(string folder)
    {
        await using var context = _factory.CreateContext(folder);
        return (await context.Characters.FirstAsync()).Id;
    }
}