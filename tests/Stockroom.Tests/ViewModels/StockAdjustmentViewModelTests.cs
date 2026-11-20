using Stockroom.Presentation.ViewModels;
using Stockroom.Tests.TestDoubles;

namespace Stockroom.Tests.ViewModels;

public class StockAdjustmentViewModelTests
{
    private static async Task<(TestApp App, StockAdjustmentViewModel Stock)> OpenStockPanelAsync(int quantity = 10)
    {
        var item = TestData.Item(quantity: quantity, reorderLevel: 5);
        var app = new TestApp(item);
        var editor = await app.NavigateToAsync<ItemEditorViewModel>(item.Id);
        return (app, editor.Stock!);
    }

    [Fact]
    public async Task Receive_and_Issue_need_a_positive_quantity_and_a_reason()
    {
        var (app, stock) = await OpenStockPanelAsync();
        using var _ = app;

        Assert.False(stock.ReceiveCommand.CanExecute(null));

        stock.Quantity = "abc";
        Assert.Single(stock.GetErrors(nameof(StockAdjustmentViewModel.Quantity)));

        stock.Quantity = "3";
        Assert.Empty(stock.GetErrors(nameof(StockAdjustmentViewModel.Quantity)));
        Assert.False(stock.IssueCommand.CanExecute(null));

        stock.Reason = "Job 17";
        Assert.True(stock.IssueCommand.CanExecute(null));
        Assert.True(stock.ReceiveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Receiving_updates_the_quantity_and_history_and_clears_the_inputs()
    {
        var (app, stock) = await OpenStockPanelAsync(quantity: 10);
        using var _ = app;
        stock.Quantity = "6";
        stock.Reason = "Delivery";

        await stock.ReceiveCommand.ExecuteAsync();

        Assert.Equal(16, stock.QuantityOnHand);
        Assert.Equal(6, Assert.Single(stock.Movements).Quantity);
        Assert.Equal("", stock.Quantity);
        Assert.Equal("", stock.Reason);
    }

    [Fact]
    public async Task Issuing_down_to_the_reorder_level_flags_low_stock()
    {
        var (app, stock) = await OpenStockPanelAsync(quantity: 10);
        using var _ = app;
        Assert.False(stock.IsLowStock);
        stock.Quantity = "5";
        stock.Reason = "Job 17";

        await stock.IssueCommand.ExecuteAsync();

        Assert.Equal(5, stock.QuantityOnHand);
        Assert.True(stock.IsLowStock);
        Assert.Equal(-5, Assert.Single(stock.Movements).Quantity);
    }

    [Fact]
    public async Task Issuing_more_than_is_on_hand_shows_the_reason_on_the_quantity_field()
    {
        var (app, stock) = await OpenStockPanelAsync(quantity: 2);
        using var _ = app;
        stock.Quantity = "3";
        stock.Reason = "Job 17";

        await stock.IssueCommand.ExecuteAsync();

        Assert.Contains("Only 2 on hand", Assert.Single(stock.GetErrors(nameof(StockAdjustmentViewModel.Quantity))));
        Assert.Equal(2, stock.QuantityOnHand);
        Assert.Empty(stock.Movements);
    }
}
