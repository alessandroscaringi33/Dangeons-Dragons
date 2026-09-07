using DndCompanion.Core.Locations;
using DndCompanion.Core.Mvvm;

namespace DndCompanion.ViewModels;

/// <summary>Presentation model of a single location for the locations list.</summary>
public sealed class LocationItemViewModel : ViewModelBase
{
    public LocationItemViewModel(LocationInfo location)
    {
        Location = location;
    }

    public LocationInfo Location { get; }

    public Guid Id => Location.Id;

    public string Name => Location.Name;

    public string Description => Location.Description;

    public bool HasDescription => !string.IsNullOrEmpty(Description);
}