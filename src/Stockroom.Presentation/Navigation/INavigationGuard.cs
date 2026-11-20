namespace Stockroom.Presentation.Navigation;

/// <summary>Implemented by view models that may need to stop the user leaving, such as a form with unsaved changes.</summary>
public interface INavigationGuard
{
    bool CanNavigateAway();
}
