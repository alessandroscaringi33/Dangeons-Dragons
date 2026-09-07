using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// A player character (PC) with its digital character sheet. Derived values
/// such as ability modifiers and the proficiency bonus are computed rather
/// than stored, to avoid duplicated data.
/// </summary>
public class Character : Entity
{
    public Guid CampaignId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string PlayerName { get; set; } = string.Empty;

    public string Class { get; set; } = string.Empty;

    public string Subclass { get; set; } = string.Empty;

    public string Race { get; set; } = string.Empty;

    public string Background { get; set; } = string.Empty;

    public int Level { get; set; } = 1;

    public int Experience { get; set; }

    public int Strength { get; set; } = 10;

    public int Dexterity { get; set; } = 10;

    public int Constitution { get; set; } = 10;

    public int Intelligence { get; set; } = 10;

    public int Wisdom { get; set; } = 10;

    public int Charisma { get; set; } = 10;

    public int CurrentHp { get; set; }

    public int MaxHp { get; set; }

    public int TemporaryHp { get; set; }

    public int ArmorClass { get; set; }

    /// <summary>The character's total initiative bonus (defaults to the Dexterity modifier).</summary>
    public int InitiativeModifier { get; set; }

    public int Speed { get; set; }

    public string Notes { get; set; } = string.Empty;

    public CharacterCondition Conditions { get; set; } = CharacterCondition.None;

    public List<InventoryItem> Inventory { get; set; } = new();

    public int StrengthModifier => Rules.Modifier(Strength);

    public int DexterityModifier => Rules.Modifier(Dexterity);

    public int ConstitutionModifier => Rules.Modifier(Constitution);

    public int IntelligenceModifier => Rules.Modifier(Intelligence);

    public int WisdomModifier => Rules.Modifier(Wisdom);

    public int CharismaModifier => Rules.Modifier(Charisma);

    public int ProficiencyBonus => Rules.ProficiencyBonus(Level);

    /// <summary>Total initiative: the stored bonus is the base Dexterity modifier unless overridden.</summary>
    public int Initiative => InitiativeModifier == 0 ? DexterityModifier : InitiativeModifier;

    public int PassivePerception => Rules.PassivePerception(WisdomModifier);

    public bool IsAlive => CurrentHp > 0;

    /// <summary>
    /// Applies damage, consuming temporary HP first and then real HP.
    /// Returns the amount of real HP actually lost.
    /// </summary>
    public int ApplyDamage(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var tempAbsorbed = Math.Min(TemporaryHp, amount);
        TemporaryHp -= tempAbsorbed;

        var remaining = amount - tempAbsorbed;
        var realLost = Math.Min(CurrentHp, remaining);
        CurrentHp -= realLost;
        CurrentHp = Rules.ClampToZero(CurrentHp);

        return realLost;
    }

    /// <summary>Restores HP up to the maximum.</summary>
    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHp = Rules.Clamp(CurrentHp + amount, 0, MaxHp);
    }

    public void SetTemporaryHp(int amount) => TemporaryHp = amount < 0 ? 0 : amount;

    public bool HasCondition(CharacterCondition condition) =>
        (Conditions & condition) == condition;

    public void AddCondition(CharacterCondition condition) => Conditions |= condition;

    public void RemoveCondition(CharacterCondition condition) => Conditions &= ~condition;
}
