using DndCompanion.Core.Domain;
using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Characters;

/// <summary>
/// UI-facing description of a player character. Raw values are stored while
/// derived values (ability modifiers, proficiency bonus, initiative, passive
/// perception) are computed on the fly and never duplicated in the database.
/// </summary>
public sealed class CharacterInfo
{
    public Guid Id { get; init; }

    public Guid CampaignId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string PlayerName { get; init; } = string.Empty;

    public string Class { get; init; } = string.Empty;

    public string Subclass { get; init; } = string.Empty;

    public string Race { get; init; } = string.Empty;

    public string Background { get; init; } = string.Empty;

    public int Level { get; init; } = 1;

    public int Experience { get; init; }

    public int Strength { get; init; } = 10;

    public int Dexterity { get; init; } = 10;

    public int Constitution { get; init; } = 10;

    public int Intelligence { get; init; } = 10;

    public int Wisdom { get; init; } = 10;

    public int Charisma { get; init; } = 10;

    public int CurrentHp { get; init; }

    public int MaxHp { get; init; }

    public int TemporaryHp { get; init; }

    public int ArmorClass { get; init; }

    public int InitiativeModifier { get; init; }

    public int Speed { get; init; }

    public string Notes { get; init; } = string.Empty;

    public CharacterCondition Conditions { get; init; } = CharacterCondition.None;

    public IReadOnlyList<InventoryItemInfo> Inventory { get; init; } = Array.Empty<InventoryItemInfo>();

    // Derived values, computed rather than stored.

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

    public double TotalWeight => Inventory.Sum(i => i.Weight * i.Quantity);
}