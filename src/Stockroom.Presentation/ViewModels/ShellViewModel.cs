using Stockroom.Presentation.Mvvm;
using Stockroom.Presentation.Navigation;
using Stockroom.Presentation.Services;

namespace Stockroom.Presentation.ViewModels;

/// <summary>The main window: a side menu and whichever page is current.</summary>
public sealed class ShellViewModel : ObservableObject
{
    public ShellViewModel(INavigationService navigation, IDialogService dialogs)
    {
        Navigation = navigation;
        var onError = dialogs.ErrorHandler();

        InitializeCommand = new AsyncRelayCommand(() => navigation.NavigateToAsync<DashboardViewModel>(), onError: onError);
        ShowDashboardCommand = new AsyncRelayCommand(() => navigation.NavigateToAsync<DashboardViewModel>(), onError: onError);
        ShowItemsCommand = new AsyncRelayCommand(() => navigation.NavigateToAsync<ItemListViewModel>(), onError: onError);
        GoBackCommand = new AsyncRelayCommand(navigation.GoBackAsync, () => navigation.CanGoBack, onError);

        navigation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(INavigationService.CanGoBack))
            {
                GoBackCommand.RaiseCanExecuteChanged();
            }
        };
    }

    public string Title => "Stockroom";

    public INavigationService Navigation { get; }

    /// <summary>Run once when the window has loaded. Bound with an EventTrigger behavior in XAML.</summary>
    public AsyncRelayCommand InitializeCommand { get; }

    public AsyncRelayCommand ShowDashboardCommand { get; }

    public AsyncRelayCommand ShowItemsCommand { get; }

    public AsyncRelayCommand GoBackCommand { get; }
}
