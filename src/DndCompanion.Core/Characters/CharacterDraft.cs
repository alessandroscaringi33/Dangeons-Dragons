using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Characters;

/// <summary>
/// Editable profile of a character used to create or update it. Derived values
/// are never part of the draft: they are always recomputed.
/// </summary>
public sealed class CharacterDraft
{
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

    public int InitiativeModifier { get; set; }

    public int Speed { get; set; }

    public string Notes { get; set; } = string.Empty;

    public CharacterCondition Conditions { get; set; } = CharacterCondition.None;
}