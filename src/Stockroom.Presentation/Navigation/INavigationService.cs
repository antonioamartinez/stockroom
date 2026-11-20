using System.ComponentModel;

namespace Stockroom.Presentation.Navigation;

/// <summary>
/// View-model-first navigation. Callers ask for a view model type; the view is picked by WPF from
/// a DataTemplate for that type, so no view model ever references a view.
/// </summary>
public interface INavigationService : INotifyPropertyChanged
{
    /// <summary>The view model on screen. The shell binds a ContentControl to this.</summary>
    object? Current { get; }

    bool CanGoBack { get; }

    /// <returns>False when the current page refused to be left (for example, unsaved changes the user chose to keep).</returns>
    Task<bool> NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : class;

    Task<bool> GoBackAsync();
}
