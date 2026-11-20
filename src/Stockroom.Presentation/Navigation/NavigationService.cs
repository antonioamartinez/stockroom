using Stockroom.Presentation.Mvvm;

namespace Stockroom.Presentation.Navigation;

/// <summary>
/// Resolves a fresh view model from the container for every navigation. The back stack stores the
/// view model type and its parameter, not the instance, so going back reloads current data and no
/// stale page is kept alive.
/// </summary>
public sealed class NavigationService(IServiceProvider services) : ObservableObject, INavigationService
{
    private readonly Stack<Destination> _history = new();
    private Destination? _currentDestination;
    private object? _current;

    public object? Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    public bool CanGoBack => _history.Count > 0;

    public async Task<bool> NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : class
    {
        if (!CanLeaveCurrent())
        {
            return false;
        }

        var destination = new Destination(typeof(TViewModel), parameter);

        // Asking for the page already on screen reloads it without growing the history.
        if (_currentDestination is not null && _currentDestination != destination)
        {
            _history.Push(_currentDestination);
            OnPropertyChanged(nameof(CanGoBack));
        }

        await ShowAsync(destination);
        return true;
    }

    public async Task<bool> GoBackAsync()
    {
        if (!CanGoBack || !CanLeaveCurrent())
        {
            return false;
        }

        var previous = _history.Pop();
        OnPropertyChanged(nameof(CanGoBack));
        await ShowAsync(previous);
        return true;
    }

    private bool CanLeaveCurrent() => Current is not INavigationGuard guard || guard.CanNavigateAway();

    private async Task ShowAsync(Destination destination)
    {
        var viewModel = services.GetService(destination.ViewModelType)
            ?? throw new InvalidOperationException($"{destination.ViewModelType.Name} is not registered with the service provider.");

        _currentDestination = destination;
        Current = viewModel;

        if (viewModel is INavigationAware aware)
        {
            await aware.OnNavigatedToAsync(destination.Parameter);
        }
    }

    private sealed record Destination(Type ViewModelType, object? Parameter);
}
