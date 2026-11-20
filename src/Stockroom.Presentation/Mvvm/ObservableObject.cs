using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Stockroom.Presentation.Mvvm;

/// <summary>Base class for anything a view binds to.</summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets the field and raises <see cref="PropertyChanged"/> only when the value actually changes.
    /// Returns true when it changed, so callers can react (re-filter, refresh a command) without a second comparison.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
