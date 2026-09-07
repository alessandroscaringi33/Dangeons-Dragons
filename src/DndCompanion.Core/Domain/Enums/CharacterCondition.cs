namespace DndCompanion.Core.Domain.Enums;

/// <summary>Conditions that can affect a character during the game.</summary>
[Flags]
public enum CharacterCondition
{
    None = 0,
    Blinded = 1,
    Charmed = 2,
    Deafened = 4,
    Frightened = 8,
    Grappled = 16,
    Incapacitated = 32,
    Invisible = 64,
    Paralyzed = 128,
    Petrified = 256,
    Poisoned = 512,
    Prone = 1024,
    Restrained = 2048,
    Stunned = 4096,
    Unconscious = 8192,
    Exhaustion = 16384
}
