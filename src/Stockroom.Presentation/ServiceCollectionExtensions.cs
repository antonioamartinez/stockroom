using Microsoft.Extensions.DependencyInjection;
using Stockroom.Presentation.Navigation;
using Stockroom.Presentation.ViewModels;

namespace Stockroom.Presentation;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers navigation and every page view model. Pages are transient so each visit starts
    /// clean; the shell and navigation live for the whole app.
    /// </summary>
    public static IServiceCollection AddStockroomViewModels(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ItemListViewModel>();
        services.AddTransient<ItemEditorViewModel>();
        return services;
    }
}
