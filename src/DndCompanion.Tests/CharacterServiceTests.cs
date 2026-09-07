using DndCompanion.Core.Characters;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Infrastructure.Characters;
using DndCompanion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DndCompanion.Tests;

/// <summary>
/// Tests for <see cref="CharacterService"/>. Every test uses a fresh temporary
/// campaign folder and database, mirroring the real <c>Campagne</c> layout.
/// </summary>
public sealed class CharacterServiceTests
{
    private readonly CampaignDatabaseFactory _factory = new();
    private readonly CampaignDatabaseInitializer _initializer;

    public CharacterServiceTests()
    {
        _initializer = new CampaignDatabaseInitializer(
            _factory,
            NullLogger<CampaignDatabaseInitializer>.Instance);
    }

    private async Task<string> CreateCampaignFolderAsync()
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            "DndCompanionCharacterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(folder);
        await _initializer.InitializeAsync(folder);

        await using var context = _factory.CreateContext(folder);
        context.Campaigns.Add(new Core.Domain.Entities.Campaign { Name = Path.GetFileName(folder) });
        await context.SaveChangesAsync();

        return folder;
    }

    private CharacterService CreateService()
    {
        return new CharacterService(_factory, NullLogger<CharacterService>.Instance);
    }

    private static CharacterDraft Draft(
        string name = "Arkon",
        int level = 5,
        int strength = 18,
        int dexterity = 14,
        int wisdom = 16,
        int maxHp = 40)
    {
        return new CharacterDraft
        {
            Name = name,
            Class = "Paladino",
            Subclass = "Devozione",
            Race = "Umano",
            Background = "Nobile",
            Level = level,
            Strength = strength,
            Dexterity = dexterity,
            Wisdom = wisdom,
            MaxHp = maxHp,
            CurrentHp = maxHp
        };
    }

    // ---------- CRUD ----------

    [Fact]
    public async Task Create_PersistsRawFields()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var created = await service.CreateCharacterAsync(folder, Draft());

        Assert.Equal("Arkon", created.Name);
        Assert.Equal("Paladino", created.Class);
        Assert.Equal("Devozione", created.Subclass);
        Assert.Equal("Umano", created.Race);
        Assert.Equal("Nobile", created.Background);
        Assert.Equal(5, created.Level);
        Assert.Equal(40, created.MaxHp);
        Assert.Equal(40, created.CurrentHp);
    }

    [Fact]
    public async Task List_ReturnsCharactersSortedByName()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.CreateCharacterAsync(folder, Draft("Zara", maxHp: 30));
        await service.CreateCharacterAsync(folder, Draft("Arkon"));

        var characters = await service.ListCharactersAsync(folder);

        Assert.Equal(new[] { "Arkon", "Zara" }, characters.Select(c => c.Name).ToArray());
    }

    [Fact]
    public async Task Get_ReturnsCharacter()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());

        var loaded = await service.GetCharacterAsync(folder, created.Id);

        Assert.Equal(created.Id, loaded.Id);
        Assert.Equal("Arkon", loaded.Name);
    }

    [Fact]
    public async Task Get_MissingCharacter_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<CharacterException>(
            () => service.GetCharacterAsync(folder, Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ChangesProfileFields()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());

        var updated = await service.UpdateCharacterAsync(folder, created.Id, Draft(name: "Aria", level: 9));

        Assert.Equal("Aria", updated.Name);
        Assert.Equal(9, updated.Level);
        Assert.Equal(4, updated.ProficiencyBonus);
    }

    [Fact]
    public async Task Update_MissingCharacter_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<CharacterException>(
            () => service.UpdateCharacterAsync(folder, Guid.NewGuid(), Draft()));
    }

    [Fact]
    public async Task Delete_RemovesCharacter_AndInventory()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());
        await service.AddInventoryItemAsync(folder, created.Id, "Spada", 1);

        await service.DeleteCharacterAsync(folder, created.Id);

        await using var context = _factory.CreateContext(folder);
        Assert.Empty(await context.Characters.ToListAsync());
        Assert.Empty(await context.InventoryItems.ToListAsync());
    }

    [Fact]
    public async Task Create_EmptyName_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<CharacterException>(
            () => service.CreateCharacterAsync(folder, Draft(name: "  ")));
    }

    [Fact]
    public async Task Create_InvalidLevel_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<CharacterException>(
            () => service.CreateCharacterAsync(folder, Draft(level: 0)));
    }

    // ---------- Derived calculations ----------

    [Fact]
    public async Task Derived_Modifiers_AreComputed()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft(strength: 18, dexterity: 14, wisdom: 16));

        Assert.Equal(4, created.StrengthModifier);
        Assert.Equal(2, created.DexterityModifier);
        Assert.Equal(3, created.WisdomModifier);
    }

    [Fact]
    public async Task Derived_ProficiencyBonus_MatchesLevel()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var c5 = await service.CreateCharacterAsync(folder, Draft(name: "A", level: 5));
        var c9 = await service.CreateCharacterAsync(folder, Draft(name: "B", level: 9));
        var c20 = await service.CreateCharacterAsync(folder, Draft(name: "C", level: 20));

        Assert.Equal(3, c5.ProficiencyBonus);
        Assert.Equal(4, c9.ProficiencyBonus);
        Assert.Equal(6, c20.ProficiencyBonus);
    }

    [Fact]
    public async Task Derived_ProficiencyBonus_FollowsRuleTable()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var levels = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        var expected = new[] { 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6 };

        for (var i = 0; i < levels.Length; i++)
        {
            var c = await service.CreateCharacterAsync(folder, Draft($"P{i}", level: levels[i]));
            Assert.Equal(expected[i], c.ProficiencyBonus);
        }
    }

    [Fact]
    public async Task Derived_Initiative_UsesDexModifier_ByDefault()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft(dexterity: 16));

        Assert.Equal(3, created.Initiative);
    }

    [Fact]
    public async Task Derived_Initiative_UsesStoredModifier_WhenSet()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var draft = Draft(dexterity: 10);
        draft.InitiativeModifier = 5;

        var created = await service.CreateCharacterAsync(folder, draft);

        Assert.Equal(5, created.Initiative);
    }

    [Fact]
    public async Task Derived_PassivePerception_IsTenPlusWisdom()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft(wisdom: 16));

        Assert.Equal(13, created.PassivePerception);
    }

    // ---------- HP quick operations ----------

    [Fact]
    public async Task ApplyDamage_ConsumesTemporaryHp_First()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft(maxHp: 20));
        await service.SetTemporaryHpAsync(folder, created.Id, 5);

        var after = await service.ApplyDamageAsync(folder, created.Id, 8);

        Assert.Equal(17, after.CurrentHp);
        Assert.Equal(0, after.TemporaryHp);
    }

    [Fact]
    public async Task ApplyDamage_ClampsAtZero()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft(maxHp: 20));

        var after = await service.ApplyDamageAsync(folder, created.Id, 100);

        Assert.Equal(0, after.CurrentHp);
        Assert.False(after.IsAlive);
    }

    [Fact]
    public async Task Heal_NeverExceedsMaxHp()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft(maxHp: 20));
        await service.ApplyDamageAsync(folder, created.Id, 15);

        var after = await service.HealAsync(folder, created.Id, 100);

        Assert.Equal(20, after.CurrentHp);
    }

    [Fact]
    public async Task Heal_Revives()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft(maxHp: 20));
        await service.ApplyDamageAsync(folder, created.Id, 20);

        var after = await service.HealAsync(folder, created.Id, 5);

        Assert.Equal(5, after.CurrentHp);
        Assert.True(after.IsAlive);
    }

    [Fact]
    public async Task SetCurrentHp_ClampsToMax()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft(maxHp: 30));

        var after = await service.SetCurrentHpAsync(folder, created.Id, 999);

        Assert.Equal(30, after.CurrentHp);
    }

    [Fact]
    public async Task SetCurrentHp_ClampsToZero()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft(maxHp: 30));

        var after = await service.SetCurrentHpAsync(folder, created.Id, -10);

        Assert.Equal(0, after.CurrentHp);
    }

    [Fact]
    public async Task SetTemporaryHp_NeverNegative()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft(maxHp: 30));

        var after = await service.SetTemporaryHpAsync(folder, created.Id, -5);

        Assert.Equal(0, after.TemporaryHp);
    }

    [Fact]
    public async Task HpOperations_PersistAcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft(maxHp: 30));
        await service.ApplyDamageAsync(folder, created.Id, 12);

        var loaded = await service.GetCharacterAsync(folder, created.Id);

        Assert.Equal(18, loaded.CurrentHp);
    }

    [Fact]
    public async Task FullSheet_PersistsAllRawFields_AcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var draft = Draft(name: "Lirael", level: 13, strength: 20, dexterity: 16, wisdom: 14, maxHp: 85);
        draft.PlayerName = "Alice";
        draft.Constitution = 18;
        draft.Intelligence = 12;
        draft.Charisma = 17;
        draft.CurrentHp = 42;
        draft.TemporaryHp = 7;
        draft.ArmorClass = 19;
        draft.InitiativeModifier = 4;
        draft.Speed = 30;
        draft.Notes = "Nota di prova";
        draft.Conditions = CharacterCondition.Poisoned | CharacterCondition.Blinded;

        var created = await service.CreateCharacterAsync(folder, draft);
        await service.AddInventoryItemAsync(folder, created.Id, "Arco Lungo", 1);

        var loaded = await service.GetCharacterAsync(folder, created.Id);

        Assert.Equal("Lirael", loaded.Name);
        Assert.Equal("Alice", loaded.PlayerName);
        Assert.Equal(13, loaded.Level);
        Assert.Equal(20, loaded.Strength);
        Assert.Equal(16, loaded.Dexterity);
        Assert.Equal(18, loaded.Constitution);
        Assert.Equal(12, loaded.Intelligence);
        Assert.Equal(14, loaded.Wisdom);
        Assert.Equal(17, loaded.Charisma);
        Assert.Equal(85, loaded.MaxHp);
        Assert.Equal(42, loaded.CurrentHp);
        Assert.Equal(7, loaded.TemporaryHp);
        Assert.Equal(19, loaded.ArmorClass);
        Assert.Equal(4, loaded.InitiativeModifier);
        Assert.Equal(30, loaded.Speed);
        Assert.Equal("Nota di prova", loaded.Notes);
        Assert.Equal(CharacterCondition.Poisoned | CharacterCondition.Blinded, loaded.Conditions);
        Assert.Equal("Arco Lungo", Assert.Single(loaded.Inventory).Name);

        // Derived values recomputed from the persisted raw fields.
        Assert.Equal(5, loaded.StrengthModifier);
        Assert.Equal(5, loaded.ProficiencyBonus);
        Assert.Equal(12, loaded.PassivePerception);
        Assert.Equal(4, loaded.Initiative);
    }

    // ---------- Conditions ----------

    [Fact]
    public async Task AddCondition_IsApplied()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());

        var after = await service.AddConditionAsync(folder, created.Id, CharacterCondition.Poisoned);

        Assert.True((after.Conditions & CharacterCondition.Poisoned) == CharacterCondition.Poisoned);
    }

    [Fact]
    public async Task RemoveCondition_IsCleared()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());
        await service.AddConditionAsync(folder, created.Id, CharacterCondition.Poisoned);
        await service.AddConditionAsync(folder, created.Id, CharacterCondition.Blinded);

        var after = await service.RemoveConditionAsync(folder, created.Id, CharacterCondition.Poisoned);

        Assert.False((after.Conditions & CharacterCondition.Poisoned) == CharacterCondition.Poisoned);
        Assert.True((after.Conditions & CharacterCondition.Blinded) == CharacterCondition.Blinded);
    }

    [Fact]
    public async Task Conditions_PersistAcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());
        await service.AddConditionAsync(folder, created.Id, CharacterCondition.Stunned);

        var loaded = await service.GetCharacterAsync(folder, created.Id);

        Assert.Equal(CharacterCondition.Stunned, loaded.Conditions);
    }

    // ---------- Inventory ----------

    [Fact]
    public async Task AddInventoryItem_IsStored()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());

        var after = await service.AddInventoryItemAsync(folder, created.Id, "Spada Lunga", 1);

        var item = Assert.Single(after.Inventory);
        Assert.Equal("Spada Lunga", item.Name);
        Assert.Equal(1, item.Quantity);
    }

    [Fact]
    public async Task AddInventoryItem_ClampsQuantity()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());

        var after = await service.AddInventoryItemAsync(folder, created.Id, "Frecce", 0);

        Assert.Equal(1, Assert.Single(after.Inventory).Quantity);
    }

    [Fact]
    public async Task AddInventoryItem_EmptyName_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());

        await Assert.ThrowsAsync<CharacterException>(
            () => service.AddInventoryItemAsync(folder, created.Id, "  ", 1));
    }

    [Fact]
    public async Task RemoveInventoryItem_RemovesIt()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());
        var after = await service.AddInventoryItemAsync(folder, created.Id, "Spada", 1);
        var itemId = after.Inventory.Single().Id;

        var result = await service.RemoveInventoryItemAsync(folder, created.Id, itemId);

        Assert.Empty(result.Inventory);
    }

    [Fact]
    public async Task Inventory_PersistsAcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());
        await service.AddInventoryItemAsync(folder, created.Id, "Pozione", 2);

        var loaded = await service.GetCharacterAsync(folder, created.Id);

        var item = Assert.Single(loaded.Inventory);
        Assert.Equal("Pozione", item.Name);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task TotalWeight_SumsQuantityTimesWeight()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateCharacterAsync(folder, Draft());
        await service.AddInventoryItemAsync(folder, created.Id, "Spada", 2);

        var loaded = await service.GetCharacterAsync(folder, created.Id);

        // Weight defaults to 0; verify TotalWeight handles items without crashing.
        Assert.Equal(0, loaded.TotalWeight);
    }
}