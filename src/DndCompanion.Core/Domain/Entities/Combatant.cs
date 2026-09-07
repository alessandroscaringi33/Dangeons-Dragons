using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// A participant in a <see cref="Combat"/>, backed by either a character or
/// an NPC. Exactly one of the two references is set.
/// </summary>
public class Combatant : Entity
{
    public Guid CombatId { get; set; }

    public Guid? CharacterId { get; set; }

    public Guid? NpcId { get; set; }

    public CombatantType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Initiative { get; set; }

    public int CurrentHp { get; set; }

    public int MaxHp { get; set; }

    public int ArmorClass { get; set; }

    /// <summary>Whether the combatant is still in the fight.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Position in the initiative order (0-based).</summary>
    public int TurnOrder { get; set; }

    public bool IsDead => CurrentHp <= 0;

    /// <summary>Applies damage, marking the combatant dead at zero HP.</summary>
    public int ApplyDamage(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var lost = Math.Min(CurrentHp, amount);
        CurrentHp = Rules.ClampToZero(CurrentHp - lost);
        return lost;
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHp = Rules.Clamp(CurrentHp + amount, 0, MaxHp);
    }

    public void RemoveFromCombat() => IsActive = false;
}
