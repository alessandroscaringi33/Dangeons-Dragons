namespace DndCompanion.Core.Npcs;

/// <summary>
/// Exception thrown by the NPC service when an NPC cannot be created, updated,
/// deleted, searched, linked or when a quick HP operation fails.
/// </summary>
public sealed class NpcException : Exception
{
    public NpcException(string message)
        : base(message)
    {
    }

    public NpcException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}