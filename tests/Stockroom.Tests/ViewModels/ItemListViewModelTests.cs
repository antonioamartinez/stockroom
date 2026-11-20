using Stockroom.Presentation.ViewModels;
using Stockroom.Tests.TestDoubles;

namespace Stockroom.Tests.ViewModels;

public class ItemListViewModelTests
{
    private static TestApp CreateApp() => new(
        TestData.Item(sku: "BOLT-M6", name: "M6 hex bolt", category: "Fasteners", quantity: 100, reorderLevel: 20),
        TestData.Item(sku: "TAPE-BLU", name: "Painter's tape", category: "Consumables", quantity: 2, reorderLevel: 5),
        TestData.Item(sku: "GLUE-PVA", name: "Wood glue", category: "Consumables", quantity: 9, reorderLevel: 3));

    [Theory]
    [InlineData("bolt", new[] { "BOLT-M6" })]
    [InlineData("  TAPE ", new[] { "TAPE-BLU" })]
    [InlineData("consumables", new[] { "TAPE-BLU", "GLUE-PVA" })]
    [InlineData("", new[] { "BOLT-M6", "TAPE-BLU", "GLUE-PVA" })]
    public async Task Search_matches_sku_name_or_category_ignoring_case(string search, string[] expectedSkus)
    {
        using var app = CreateApp();
        var list = await app.NavigateToAsync<ItemListViewModel>();

        list.SearchText = search;

        Assert.Equal(expectedSkus.Order(), list.Items.Select(i => i.Sku).Order());
        Assert.Equal($"Showing {expectedSkus.Length} of 3 items", list.Summary);
    }

    [Fact]
    public async Task Low_stock_filter_combines_with_search()
    {
        using var app = CreateApp();
        var list = await app.NavigateToAsync<ItemListViewModel>();

        list.LowStockOnly = true;
        Assert.Equal(["TAPE-BLU"], list.Items.Select(i => i.Sku));

        list.SearchText = "glue";
        Assert.Empty(list.Items);

        list.ClearSearchCommand.Execute(null);
        Assert.Equal("", list.SearchText);
        Assert.Single(list.Items);
    }

    [Fact]
    public async Task Selection_survives_filtering_while_the_item_is_still_visible()
    {
        using var app = CreateApp();
        var list = await app.NavigateToAsync<ItemListViewModel>();
        list.SelectedItem = list.Items.Single(i => i.Sku == "TAPE-BLU");

        list.SearchText = "consumables";
        Assert.Equal("TAPE-BLU", list.SelectedItem?.Sku);

        list.SearchText = "bolt";
        Assert.Null(list.SelectedItem);
        Assert.False(list.EditCommand.CanExecute(null));
    }

    [Fact]
    public async Task Edit_opens_the_selected_item()
    {
        using var app = CreateApp();
        var list = await app.NavigateToAsync<ItemListViewModel>();
        list.SelectedItem = list.Items.Single(i => i.Sku == "GLUE-PVA");

        await list.EditCommand.ExecuteAsync();

        Assert.Equal("Wood glue", Assert.IsType<ItemEditorViewModel>(app.Navigation.Current).Name);
    }

    [Fact]
    public async Task Delete_does_nothing_when_the_user_says_no()
    {
        using var app = CreateApp();
        app.Dialogs.ConfirmResult = false;
        var list = await app.NavigateToAsync<ItemListViewModel>();
        list.SelectedItem = list.Items[0];

        await list.DeleteCommand.ExecuteAsync();

        Assert.Single(app.Dialogs.Confirmations);
        Assert.Equal(3, list.Items.Count);
    }

    [Fact]
    public async Task Delete_removes_the_item_after_confirmation()
    {
        using var app = CreateApp();
        var list = await app.NavigateToAsync<ItemListViewModel>();
        list.SelectedItem = list.Items.Single(i => i.Sku == "BOLT-M6");

        await list.DeleteCommand.ExecuteAsync();

        Assert.DoesNotContain(list.Items, i => i.Sku == "BOLT-M6");
        Assert.Equal(2, (await app.Inventory.GetItemsAsync()).Count);
        Assert.Null(list.SelectedItem);
    }
}
