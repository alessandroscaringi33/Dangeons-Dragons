using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Domain.Entities;

/// <summary>
/// An encounter between multiple combatants, tracked round by round.
/// </summary>
public class Combat : Entity
{
    public Guid SessionId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public int CurrentRound { get; set; } = 1;

    /// <summary>Index (into the active ordered combatants) of the acting combatant.</summary>
    public int CurrentTurnIndex { get; set; }

    public bool IsActive { get; set; } = true;

    public List<Combatant> Combatants { get; set; } = new();

    /// <summary>Combatants still in the fight, ordered by their turn order.</summary>
    public IReadOnlyList<Combatant> ActiveCombatantsInOrder =>
        Combatants.Where(c => c.IsActive)
                  .OrderBy(c => c.TurnOrder)
                  .ToList();

    /// <summary>The combatant currently acting, or null if none.</summary>
    public Combatant? CurrentCombatant
    {
        get
        {
            var active = ActiveCombatantsInOrder;
            if (active.Count == 0 || CurrentTurnIndex < 0 || CurrentTurnIndex >= active.Count)
            {
                return null;
            }

            return active[CurrentTurnIndex];
        }
    }

    /// <summary>Adds a combatant and re-sorts the turn order.</summary>
    public void AddCombatant(Combatant combatant)
    {
        Combatants.Add(combatant);
        SortByInitiative();
    }

    public void RemoveCombatant(Combatant combatant)
    {
        Combatants.Remove(combatant);
        SortByInitiative();
    }

    /// <summary>Orders combatants by initiative (descending) and resets the turn.</summary>
    public void SortByInitiative()
    {
        var ordered = Combatants
            .OrderByDescending(c => c.Initiative)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].TurnOrder = i;
        }

        CurrentTurnIndex = 0;
    }

    /// <summary>Advances to the next active combatant, wrapping at the end.</summary>
    public void NextTurn()
    {
        var active = ActiveCombatantsInOrder;
        if (active.Count == 0)
        {
            return;
        }

        if (CurrentTurnIndex + 1 >= active.Count)
        {
            CurrentTurnIndex = 0;
            CurrentRound++;
        }
        else
        {
            CurrentTurnIndex++;
        }
    }

    /// <summary>Moves the pointer directly to a specific combatant (index).</summary>
    public void SetCurrentTurn(int index)
    {
        var active = ActiveCombatantsInOrder;
        if (active.Count == 0 || index < 0 || index >= active.Count)
        {
            return;
        }

        CurrentTurnIndex = index;
    }

    public void End()
    {
        IsActive = false;
        EndedAt = DateTime.UtcNow;
    }
}
