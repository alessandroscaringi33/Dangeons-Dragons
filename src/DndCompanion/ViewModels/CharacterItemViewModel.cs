using DndCompanion.Core.Characters;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.Mvvm;

namespace DndCompanion.ViewModels;

/// <summary>
/// Presentation model of a single character for the characters list, including
/// a compact HP summary used by the quick HP controls.
/// </summary>
public sealed class CharacterItemViewModel : ViewModelBase
{
    private static readonly Color AliveColor = Color.FromArgb("#4CAF50");
    private static readonly Color DownColor = Color.FromArgb("#F44336");
    private static readonly Color NeutralColor = Color.FromArgb("#9E9E9E");

    public CharacterItemViewModel(CharacterInfo character)
    {
        Character = character;
    }

    public CharacterInfo Character { get; }

    public Guid Id => Character.Id;

    public string Name => Character.Name;

    public string PlayerName => Character.PlayerName;

    public string RaceClassText
    {
        get
        {
            var parts = new[] { Character.Race, Character.Class, Character.Subclass }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(" · ", parts);
        }
    }

    public string LevelText => $"Livello {Character.Level}";

    public string HpText => $"{Character.CurrentHp} / {Character.MaxHp}";

    public string TemporaryHpText => Character.TemporaryHp > 0 ? $" (+{Character.TemporaryHp} temp)" : string.Empty;

    public string HpStatusText => Character.IsAlive ? "In piedi" : "A terra";

    public Color HpStatusColor => Character.IsAlive ? AliveColor : DownColor;

    public Color HpTextColor => Character.IsAlive ? Color.FromArgb("#E8E8E8") : DownColor;

    public double HpProgress
    {
        get
        {
            if (Character.MaxHp <= 0)
            {
                return 0;
            }

            var ratio = Character.CurrentHp / (double)Character.MaxHp;
            return Math.Clamp(ratio, 0, 1);
        }
    }

    public Color HpProgressColor
    {
        get
        {
            if (HpProgress <= 0.25)
            {
                return DownColor;
            }

            if (HpProgress <= 0.5)
            {
                return Color.FromArgb("#FFB74D");
            }

            return AliveColor;
        }
    }

    public string ConditionsText
    {
        get
        {
            if (Character.Conditions == CharacterCondition.None)
            {
                return string.Empty;
            }

            var names = Enum.GetValues<CharacterCondition>()
                .Where(c => c != CharacterCondition.None && (Character.Conditions & c) == c)
                .Select(c => c switch
                {
                    CharacterCondition.Blinded => "Cieco",
                    CharacterCondition.Charmed => "Amaliato",
                    CharacterCondition.Deafened => "Assordato",
                    CharacterCondition.Frightened => "Impaurito",
                    CharacterCondition.Grappled => "Afferrato",
                    CharacterCondition.Incapacitated => "Incapacitato",
                    CharacterCondition.Invisible => "Invisibile",
                    CharacterCondition.Paralyzed => "Paralizzato",
                    CharacterCondition.Petrified => "Pietrificato",
                    CharacterCondition.Poisoned => "Avvelenato",
                    CharacterCondition.Prone => "Prono",
                    CharacterCondition.Restrained => "Trattenuto",
                    CharacterCondition.Stunned => "Stordito",
                    CharacterCondition.Unconscious => "Incosciente",
                    CharacterCondition.Exhaustion => "Affaticamento",
                    _ => c.ToString()
                });

            return string.Join(", ", names);
        }
    }

    public bool HasConditions => !string.IsNullOrEmpty(ConditionsText);

    public Color ConditionsColor => string.IsNullOrEmpty(ConditionsText) ? NeutralColor : Color.FromArgb("#FF9800");

    /// <summary>Quick damage/heal amount used by the in-session controls.</summary>
    public string QuickAmount { get; set; } = "1";
}