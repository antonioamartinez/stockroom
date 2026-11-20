namespace Stockroom.Presentation.Navigation;

/// <summary>Implemented by view models that load data when shown. The parameter is whatever the caller passed.</summary>
public interface INavigationAware
{
    Task OnNavigatedToAsync(object? parameter);
}
