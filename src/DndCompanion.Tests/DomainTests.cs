using DndCompanion.Core.Domain;
using DndCompanion.Core.Domain.Entities;
using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Tests;

public sealed class RulesTests
{
    [Theory]
    [InlineData(1, -5)]
    [InlineData(8, -1)]
    [InlineData(9, -1)]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    [InlineData(14, 2)]
    [InlineData(18, 4)]
    [InlineData(20, 5)]
    public void Modifier_IsCalculated(int score, int expected)
    {
        Assert.Equal(expected, Rules.Modifier(score));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(8, 3)]
    [InlineData(9, 4)]
    [InlineData(12, 4)]
    [InlineData(13, 5)]
    [InlineData(17, 6)]
    public void ProficiencyBonus_ByLevel(int level, int expected)
    {
        Assert.Equal(expected, Rules.ProficiencyBonus(level));
    }

    [Fact]
    public void PassivePerception_IsTenPlusWisdom()
    {
        Assert.Equal(13, Rules.PassivePerception(3));
    }

    [Fact]
    public void ClampToZero_NeverReturnsNegative()
    {
        Assert.Equal(0, Rules.ClampToZero(-5));
        Assert.Equal(7, Rules.ClampToZero(7));
    }
}

public sealed class CharacterTests
{
    private static Character CreateCharacter()
    {
        return new Character
        {
            MaxHp = 20,
            CurrentHp = 20,
            Strength = 18,
            Dexterity = 14,
            Wisdom = 16,
            Level = 5
        };
    }

    [Fact]
    public void DerivedValues_AreComputed()
    {
        var c = CreateCharacter();

        Assert.Equal(4, c.StrengthModifier);
        Assert.Equal(2, c.DexterityModifier);
        Assert.Equal(3, c.WisdomModifier);
        Assert.Equal(3, c.ProficiencyBonus);
        Assert.Equal(13, c.PassivePerception);
    }

    [Fact]
    public void Initiative_UsesDexterityModifier_ByDefault()
    {
        var c = CreateCharacter();
        Assert.Equal(2, c.Initiative);
    }

    [Fact]
    public void Initiative_UsesStoredModifier_WhenSet()
    {
        var c = CreateCharacter();
        c.InitiativeModifier = 5;
        Assert.Equal(5, c.Initiative);
    }

    [Fact]
    public void ApplyDamage_ConsumesTemporaryHp_First()
    {
        var c = CreateCharacter();
        c.SetTemporaryHp(5);

        var realLost = c.ApplyDamage(8);

        Assert.Equal(3, realLost);
        Assert.Equal(17, c.CurrentHp);
        Assert.Equal(0, c.TemporaryHp);
    }

    [Fact]
    public void ApplyDamage_ClampsAtZero()
    {
        var c = CreateCharacter();
        c.ApplyDamage(50);
        Assert.Equal(0, c.CurrentHp);
        Assert.False(c.IsAlive);
    }

    [Fact]
    public void Heal_NeverExceedsMaxHp()
    {
        var c = CreateCharacter();
        c.CurrentHp = 5;
        c.Heal(100);
        Assert.Equal(20, c.CurrentHp);
    }

    [Fact]
    public void Conditions_CanBeAddedAndRemoved()
    {
        var c = CreateCharacter();
        c.AddCondition(CharacterCondition.Poisoned);
        Assert.True(c.HasCondition(CharacterCondition.Poisoned));

        c.AddCondition(CharacterCondition.Blinded);
        Assert.True(c.HasCondition(CharacterCondition.Blinded));

        c.RemoveCondition(CharacterCondition.Poisoned);
        Assert.False(c.HasCondition(CharacterCondition.Poisoned));
        Assert.True(c.HasCondition(CharacterCondition.Blinded));
    }
}

public sealed class DiceRollTests
{
    [Fact]
    public void Total_IsSumOfResultsPlusModifier()
    {
        var roll = new DiceRoll
        {
            Results = new List<int> { 4, 3 },
            Modifier = 2
        };

        Assert.Equal(9, roll.Total);
    }

    [Fact]
    public void BestResult_ReturnsMaximum()
    {
        var roll = new DiceRoll { Results = new List<int> { 3, 17 } };
        Assert.Equal(17, roll.BestResult);
        Assert.Equal(3, roll.WorstResult);
    }

    [Fact]
    public void Total_OfEmptyRoll_IsModifier()
    {
        var roll = new DiceRoll { Modifier = 5 };
        Assert.Equal(5, roll.Total);
    }
}

public sealed class CombatTests
{
    private static Combatant CreateCombatant(string name, int initiative)
    {
        return new Combatant
        {
            Name = name,
            Initiative = initiative,
            MaxHp = 10,
            CurrentHp = 10,
            Type = CombatantType.Npc
        };
    }

    [Fact]
    public void SortByInitiative_OrdersDescending()
    {
        var combat = new Combat();
        combat.AddCombatant(CreateCombatant("Goblin", 12));
        combat.AddCombatant(CreateCombatant("Arkon", 20));
        combat.AddCombatant(CreateCombatant("Orc", 15));

        var order = combat.ActiveCombatantsInOrder.Select(c => c.Name).ToList();

        Assert.Equal(new[] { "Arkon", "Orc", "Goblin" }, order);
        Assert.Equal("Arkon", combat.CurrentCombatant?.Name);
    }

    [Fact]
    public void NextTurn_AdvancesAndWrapsRound()
    {
        var combat = new Combat();
        combat.AddCombatant(CreateCombatant("A", 10));
        combat.AddCombatant(CreateCombatant("B", 9));

        Assert.Equal(1, combat.CurrentRound);

        combat.NextTurn();
        Assert.Equal("B", combat.CurrentCombatant?.Name);
        Assert.Equal(1, combat.CurrentRound);

        combat.NextTurn();
        Assert.Equal("A", combat.CurrentCombatant?.Name);
        Assert.Equal(2, combat.CurrentRound);
    }

    [Fact]
    public void RemovedCombatant_IsSkipped()
    {
        var combat = new Combat();
        var a = CreateCombatant("A", 10);
        var b = CreateCombatant("B", 9);
        var c = CreateCombatant("C", 8);
        combat.AddCombatant(a);
        combat.AddCombatant(b);
        combat.AddCombatant(c);

        c.RemoveFromCombat();

        combat.NextTurn();
        combat.NextTurn();

        Assert.Equal("A", combat.CurrentCombatant?.Name);
    }
}

public sealed class NpcTests
{
    [Fact]
    public void ApplyDamage_ToZero_IsAliveFalse()
    {
        var npc = new Npc { MaxHp = 10, CurrentHp = 10 };
        npc.ApplyDamage(10);

        Assert.Equal(0, npc.CurrentHp);
        Assert.False(npc.IsAlive);
    }

    [Fact]
    public void Heal_Revives()
    {
        var npc = new Npc { MaxHp = 10, CurrentHp = 10 };
        npc.ApplyDamage(10);
        Assert.False(npc.IsAlive);

        npc.Heal(5);
        Assert.Equal(5, npc.CurrentHp);
        Assert.True(npc.IsAlive);
    }
}
