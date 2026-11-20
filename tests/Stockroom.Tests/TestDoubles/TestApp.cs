using Microsoft.Extensions.DependencyInjection;
using Stockroom.Core.Models;
using Stockroom.Core.Persistence;
using Stockroom.Core.Services;
using Stockroom.Presentation;
using Stockroom.Presentation.Navigation;
using Stockroom.Presentation.Services;

namespace Stockroom.Tests.TestDoubles;

/// <summary>
/// The same container the WPF app builds, with the file store, clock and dialogs swapped for test
/// versions. View model tests drive the app through navigation the way the UI does.
/// </summary>
public sealed class TestApp : IDisposable
{
    public TestApp(params InventoryItem[] seed)
    {
        Repository = new InMemoryInventoryRepository(seed);
        Services = new ServiceCollection()
            .AddSingleton<TimeProvider>(Clock)
            .AddSingleton<IInventoryRepository>(Repository)
            .AddSingleton<InventoryService>()
            .AddSingleton<IDialogService>(Dialogs)
            .AddStockroomViewModels()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public FakeDialogService Dialogs { get; } = new();

    public FixedTimeProvider Clock { get; } = new();

    public InMemoryInventoryRepository Repository { get; }

    public ServiceProvider Services { get; }

    public InventoryService Inventory => Services.GetRequiredService<InventoryService>();

    public INavigationService Navigation => Services.GetRequiredService<INavigationService>();

    public async Task<T> NavigateToAsync<T>(object? parameter = null) where T : class
    {
        Assert.True(await Navigation.NavigateToAsync<T>(parameter));
        return Assert.IsType<T>(Navigation.Current);
    }

    public void Dispose() => Services.Dispose();
}
