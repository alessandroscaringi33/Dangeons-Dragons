namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// A non-player character controlled by the DM.
/// </summary>
public class Npc : Entity
{
    public Guid CampaignId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int CurrentHp { get; set; }

    public int MaxHp { get; set; }

    public int ArmorClass { get; set; }

    public int InitiativeModifier { get; set; }

    public string Notes { get; set; } = string.Empty;

    /// <summary>Whether the NPC is alive. Kept explicit because the DM may
    /// mark an NPC dead even when it still has HP.</summary>
    public bool IsAlive { get; set; } = true;

    /// <summary>Whether the NPC has been revealed to the players.</summary>
    public bool IsKnown { get; set; }

    /// <summary>
    /// Applies damage and marks the NPC dead when HP reaches zero.
    /// Returns the amount of HP actually lost.
    /// </summary>
    public int ApplyDamage(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var lost = Math.Min(CurrentHp, amount);
        CurrentHp = Rules.ClampToZero(CurrentHp - lost);

        if (CurrentHp <= 0)
        {
            IsAlive = false;
        }

        return lost;
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHp = Rules.Clamp(CurrentHp + amount, 0, MaxHp);
        if (CurrentHp > 0)
        {
            IsAlive = true;
        }
    }
}
