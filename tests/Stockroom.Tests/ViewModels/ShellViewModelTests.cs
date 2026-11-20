using Microsoft.Extensions.DependencyInjection;
using Stockroom.Presentation.ViewModels;
using Stockroom.Tests.TestDoubles;

namespace Stockroom.Tests.ViewModels;

public class ShellViewModelTests
{
    [Fact]
    public async Task Starts_on_the_dashboard_and_enables_Back_once_there_is_history()
    {
        using var app = new TestApp(TestData.Item());
        var shell = app.Services.GetRequiredService<ShellViewModel>();
        var backChanges = 0;
        shell.GoBackCommand.CanExecuteChanged += (_, _) => backChanges++;

        await shell.InitializeCommand.ExecuteAsync();
        Assert.IsType<DashboardViewModel>(shell.Navigation.Current);
        Assert.False(shell.GoBackCommand.CanExecute(null));

        await shell.ShowItemsCommand.ExecuteAsync();
        Assert.IsType<ItemListViewModel>(shell.Navigation.Current);
        Assert.True(shell.GoBackCommand.CanExecute(null));
        Assert.True(backChanges > 0);

        await shell.GoBackCommand.ExecuteAsync();
        Assert.IsType<DashboardViewModel>(shell.Navigation.Current);
    }
}
