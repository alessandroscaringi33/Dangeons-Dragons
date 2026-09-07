namespace DndCompanion.Core.Characters;

/// <summary>
/// Exception thrown by the character service when a character cannot be
/// created, updated, deleted or when a quick HP/condition/inventory operation
/// fails.
/// </summary>
public sealed class CharacterException : Exception
{
    public CharacterException(string message)
        : base(message)
    {
    }

    public CharacterException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}