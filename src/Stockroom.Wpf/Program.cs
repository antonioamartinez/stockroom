using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Stockroom.Core.Persistence;
using Stockroom.Core.Services;
using Stockroom.Presentation;
using Stockroom.Presentation.Services;
using Stockroom.Presentation.ViewModels;
using Stockroom.Wpf.Services;

namespace Stockroom.Wpf;

/// <summary>
/// The composition root. This replaces App.xaml.cs: it builds the container, loads the XAML
/// resources, creates the window and hands it the shell view model. It is the only place that
/// knows about every layer.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var dataFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stockroom", "inventory.json");
        var isFirstRun = !File.Exists(dataFile);

        using var services = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IInventoryRepository>(new JsonInventoryRepository(dataFile))
            .AddSingleton<InventoryService>()
            .AddSingleton<IDialogService, MessageBoxDialogService>()
            .AddStockroomViewModels()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        if (isFirstRun)
        {
            // No UI thread context exists yet, so blocking here cannot deadlock.
            SampleData.SeedAsync(services.GetRequiredService<InventoryService>()).GetAwaiter().GetResult();
        }

        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        app.Resources.MergedDictionaries.Add(Load<ResourceDictionary>("Resources/Theme.xaml"));
        app.Resources.MergedDictionaries.Add(Load<ResourceDictionary>("Views/ViewTemplates.xaml"));

        // Async commands report their own errors. This is the last line of defence for anything else.
        var dialogs = services.GetRequiredService<IDialogService>();
        app.DispatcherUnhandledException += (_, e) =>
        {
            dialogs.ShowError("Unexpected error", e.Exception.Message);
            e.Handled = true;
        };

        var shell = Load<Window>("ShellWindow.xaml");
        shell.DataContext = services.GetRequiredService<ShellViewModel>();
        app.Run(shell);
    }

    /// <summary>Loads compiled XAML from this assembly. The files have no x:Class, so there is no code-behind to pair with.</summary>
    private static T Load<T>(string path) =>
        (T)Application.LoadComponent(new Uri($"/Stockroom.Wpf;component/{path}", UriKind.Relative));
}
