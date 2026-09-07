namespace DndCompanion.Core.Locations;

/// <summary>
/// Exception thrown by the location service when a location cannot be created,
/// updated or deleted.
/// </summary>
public sealed class LocationException : Exception
{
    public LocationException(string message)
        : base(message)
    {
    }

    public LocationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}