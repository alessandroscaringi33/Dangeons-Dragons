using CommunityToolkit.Mvvm.ComponentModel;

namespace DndCompanion.Core.Mvvm;

/// <summary>
/// Base class for all view models in the application.
/// Provides change notification via <see cref="ObservableObject"/>.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}