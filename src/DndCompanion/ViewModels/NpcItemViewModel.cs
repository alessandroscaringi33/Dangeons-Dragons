using DndCompanion.Core.Mvvm;
using DndCompanion.Core.Npcs;

namespace DndCompanion.ViewModels;

/// <summary>
/// Presentation model of a single NPC for the DM list, including a compact
/// HP/CA/initiative summary and alive state for quick session management.
/// </summary>
public sealed class NpcItemViewModel : ViewModelBase
{
    private static readonly Color AliveColor = Color.FromArgb("#4CAF50");
    private static readonly Color DeadColor = Color.FromArgb("#F44336");

    public NpcItemViewModel(NpcInfo npc)
    {
        Npc = npc;
    }

    public NpcInfo Npc { get; }

    public Guid Id => Npc.Id;

    public string Name => Npc.Name;

    public string RoleText => string.IsNullOrWhiteSpace(Npc.Role) ? "Nessun ruolo" : Npc.Role;

    public string HpText => $"{Npc.CurrentHp} / {Npc.MaxHp}";

    public string ArmorClassText => Npc.ArmorClass > 0 ? $"CA {Npc.ArmorClass}" : "CA —";

    public string InitiativeText => Npc.InitiativeModifier >= 0
        ? $"Iniz. +{Npc.InitiativeModifier}"
        : $"Iniz. {Npc.InitiativeModifier}";

    public string StatusText => Npc.IsAlive ? "Vivo" : "Morto";

    public Color StatusColor => Npc.IsAlive ? AliveColor : DeadColor;

    public string LinkText
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Npc.SceneTitle))
            {
                parts.Add($"Scena: {Npc.SceneTitle}");
            }

            if (!string.IsNullOrWhiteSpace(Npc.LocationName))
            {
                parts.Add($"Luogo: {Npc.LocationName}");
            }

            return parts.Count == 0 ? "Non collegato" : string.Join("  ·  ", parts);
        }
    }

    public bool IsKnownTextVisible => Npc.IsKnown;

    /// <summary>Quick damage/heal amount used by the in-session controls.</summary>
    public string QuickAmount { get; set; } = "1";
}