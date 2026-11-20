using System.Collections;
using System.ComponentModel;

namespace Stockroom.Presentation.Mvvm;

/// <summary>
/// Adds <see cref="INotifyDataErrorInfo"/> to <see cref="ObservableObject"/>. WPF reads the errors
/// through bindings and shows them next to the field; the view model owns the rules and tests can
/// read the same errors directly.
/// </summary>
public abstract class ValidatableObject : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Count > 0;

    /// <summary>Errors for one property, or for every property when the name is null or empty.</summary>
    public IReadOnlyList<string> GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return _errors.Values.SelectMany(e => e).ToList();
        }

        return _errors.TryGetValue(propertyName, out var errors) ? errors : [];
    }

    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName) => GetErrors(propertyName);

    /// <summary>Replaces the errors for a property. An empty sequence clears them.</summary>
    protected void SetErrors(string propertyName, IEnumerable<string> errors)
    {
        var newErrors = errors.ToList();
        bool changed;
        if (newErrors.Count == 0)
        {
            changed = _errors.Remove(propertyName);
        }
        else
        {
            changed = !_errors.TryGetValue(propertyName, out var existing) || !existing.SequenceEqual(newErrors);
            _errors[propertyName] = newErrors;
        }

        if (!changed)
        {
            return;
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
        OnErrorsChanged();
    }

    protected void SetError(string propertyName, string? error) =>
        SetErrors(propertyName, error is null ? [] : [error]);

    protected void ClearErrors(string propertyName) => SetErrors(propertyName, []);

    /// <summary>Hook for derived classes, typically to refresh commands whose availability depends on errors.</summary>
    protected virtual void OnErrorsChanged()
    {
    }
}
