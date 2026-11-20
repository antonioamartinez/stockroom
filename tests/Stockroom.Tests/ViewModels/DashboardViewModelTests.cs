using Stockroom.Presentation.ViewModels;
using Stockroom.Tests.TestDoubles;

namespace Stockroom.Tests.ViewModels;

public class DashboardViewModelTests
{
    [Fact]
    public async Task Shows_totals_and_low_stock_items_most_urgent_first()
    {
        using var app = new TestApp(
            TestData.Item(sku: "A", name: "Plenty", quantity: 50, reorderLevel: 5, unitCost: 2m),
            TestData.Item(sku: "B", name: "Slightly low", quantity: 4, reorderLevel: 5, unitCost: 1m),
            TestData.Item(sku: "C", name: "Very low", quantity: 0, reorderLevel: 10, unitCost: 3m));

        var dashboard = await app.NavigateToAsync<DashboardViewModel>();

        Assert.Equal(3, dashboard.ItemCount);
        Assert.Equal(54, dashboard.TotalUnits);
        Assert.Equal(104m, dashboard.StockValue);
        Assert.True(dashboard.HasLowStock);
        Assert.Equal(["C", "B"], dashboard.LowStockItems.Select(i => i.Sku));
    }

    [Fact]
    public async Task Opening_a_low_stock_item_goes_to_its_editor()
    {
        var item = TestData.Item(quantity: 1, reorderLevel: 5);
        using var app = new TestApp(item);
        var dashboard = await app.NavigateToAsync<DashboardViewModel>();
        Assert.False(dashboard.OpenSelectedCommand.CanExecute(null));

        dashboard.SelectedLowStockItem = dashboard.LowStockItems[0];
        await dashboard.OpenSelectedCommand.ExecuteAsync();

        var editor = Assert.IsType<ItemEditorViewModel>(app.Navigation.Current);
        Assert.Equal(item.Sku, editor.Sku);
    }
}
