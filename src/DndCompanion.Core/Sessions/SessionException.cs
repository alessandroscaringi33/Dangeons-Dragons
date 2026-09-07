namespace DndCompanion.Core.Sessions;

/// <summary>
/// Exception thrown by the session service when a session cannot be created,
/// opened, closed or updated.
/// </summary>
public sealed class SessionException : Exception
{
    public SessionException(string message)
        : base(message)
    {
    }

    public SessionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}