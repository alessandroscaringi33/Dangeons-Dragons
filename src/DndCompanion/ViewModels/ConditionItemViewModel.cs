using DndCompanion.Core.Mvvm;

namespace DndCompanion.ViewModels;

/// <summary>
/// Presentation model of a single toggleable condition chip on the character
/// sheet.
/// </summary>
public sealed class ConditionItemViewModel : ViewModelBase
{
    private bool _isActive;

    public ConditionItemViewModel(string name, bool isActive)
    {
        Name = name;
        _isActive = isActive;
    }

    public string Name { get; }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
            {
                return;
            }

            _isActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BackgroundColor));
            OnPropertyChanged(nameof(TextColor));
        }
    }

    public Color BackgroundColor => IsActive ? Color.FromArgb("#B08D57") : Color.FromArgb("#2E2E2E");

    public Color TextColor => IsActive ? Color.FromArgb("#1A1A1A") : Color.FromArgb("#E8E8E8");
}