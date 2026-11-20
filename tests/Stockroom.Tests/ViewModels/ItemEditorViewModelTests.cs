using System.Globalization;
using Stockroom.Core.Services;
using Stockroom.Presentation.ViewModels;
using Stockroom.Tests.TestDoubles;

namespace Stockroom.Tests.ViewModels;

public class ItemEditorViewModelTests
{
    private static void FillValidNewItem(ItemEditorViewModel editor)
    {
        editor.Sku = "NUT-M6";
        editor.Name = "M6 nut";
        editor.Category = "Fasteners";
        editor.ReorderLevel = "10";
        editor.UnitCost = "1";
        editor.OpeningQuantity = "25";
    }

    [Fact]
    public async Task A_new_form_opens_without_errors_and_with_Save_disabled()
    {
        using var app = new TestApp();

        var editor = await app.NavigateToAsync<ItemEditorViewModel>();

        Assert.True(editor.IsNew);
        Assert.Equal("New item", editor.Title);
        Assert.False(editor.HasErrors);
        Assert.False(editor.SaveCommand.CanExecute(null));
        Assert.Null(editor.Stock);
    }

    [Fact]
    public async Task Saving_with_a_missing_field_shows_its_error_and_saves_nothing()
    {
        using var app = new TestApp();
        var editor = await app.NavigateToAsync<ItemEditorViewModel>();
        editor.Name = "M6 nut";
        Assert.True(editor.SaveCommand.CanExecute(null));

        await editor.SaveCommand.ExecuteAsync();

        Assert.Equal(["SKU is required."], editor.GetErrors(nameof(ItemEditorViewModel.Sku)));
        Assert.False(editor.SaveCommand.CanExecute(null));
        Assert.Empty(await app.Inventory.GetItemsAsync());
    }

    [Theory]
    [InlineData(nameof(ItemEditorViewModel.ReorderLevel), "lots")]
    [InlineData(nameof(ItemEditorViewModel.ReorderLevel), "-1")]
    [InlineData(nameof(ItemEditorViewModel.UnitCost), "free")]
    [InlineData(nameof(ItemEditorViewModel.UnitCost), "-3")]
    [InlineData(nameof(ItemEditorViewModel.OpeningQuantity), "2.5")]
    [InlineData(nameof(ItemEditorViewModel.Sku), "NUT M6")]
    public async Task Bad_input_becomes_a_validation_error_on_that_field(string property, string input)
    {
        using var app = new TestApp();
        var editor = await app.NavigateToAsync<ItemEditorViewModel>();
        FillValidNewItem(editor);

        typeof(ItemEditorViewModel).GetProperty(property)!.SetValue(editor, input);

        Assert.Single(editor.GetErrors(property));
        Assert.False(editor.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Saving_a_new_item_stores_it_with_an_opening_balance_and_returns_to_the_list()
    {
        using var app = new TestApp();
        var list = await app.NavigateToAsync<ItemListViewModel>();
        await list.AddCommand.ExecuteAsync();
        var editor = Assert.IsType<ItemEditorViewModel>(app.Navigation.Current);
        FillValidNewItem(editor);

        await editor.SaveCommand.ExecuteAsync();

        var backOnList = Assert.IsType<ItemListViewModel>(app.Navigation.Current);
        var saved = Assert.Single(backOnList.Items);
        Assert.Equal("NUT-M6", saved.Sku);
        Assert.Equal(25, saved.QuantityOnHand);
        var opening = Assert.Single(await app.Inventory.GetMovementsAsync(saved.Id));
        Assert.Equal(InventoryService.OpeningBalanceReason, opening.Reason);
        Assert.Empty(app.Dialogs.Confirmations);
    }

    [Fact]
    public async Task A_duplicate_sku_is_shown_on_the_sku_field()
    {
        using var app = new TestApp(TestData.Item(sku: "NUT-M6"));
        var editor = await app.NavigateToAsync<ItemEditorViewModel>();
        FillValidNewItem(editor);
        editor.Sku = "nut-m6";

        await editor.SaveCommand.ExecuteAsync();

        Assert.Contains("already used", Assert.Single(editor.GetErrors(nameof(ItemEditorViewModel.Sku))));
        Assert.Same(editor, app.Navigation.Current);

        editor.Sku = "NUT-M8";
        Assert.Empty(editor.GetErrors(nameof(ItemEditorViewModel.Sku)));
    }

    [Fact]
    public async Task Editing_loads_the_item_and_its_stock_history_without_marking_it_dirty()
    {
        var item = TestData.Item(unitCost: 0.25m);
        using var app = new TestApp(item);
        await app.Inventory.AdjustStockAsync(item.Id, 5, "Delivery");

        var editor = await app.NavigateToAsync<ItemEditorViewModel>(item.Id);

        Assert.False(editor.IsNew);
        Assert.Equal("Edit item", editor.Title);
        Assert.Equal(item.Name, editor.Name);
        Assert.Equal(0.25m, decimal.Parse(editor.UnitCost, CultureInfo.CurrentCulture));
        Assert.False(editor.IsDirty);
        Assert.NotNull(editor.Stock);
        Assert.Equal(15, editor.Stock.QuantityOnHand);
        Assert.Single(editor.Stock.Movements);
    }

    [Fact]
    public async Task Leaving_with_unsaved_changes_asks_and_stays_when_the_user_says_no()
    {
        var item = TestData.Item();
        using var app = new TestApp(item);
        await app.NavigateToAsync<ItemListViewModel>();
        var editor = await app.NavigateToAsync<ItemEditorViewModel>(item.Id);
        editor.Name = "Changed";
        app.Dialogs.ConfirmResult = false;

        await editor.CancelCommand.ExecuteAsync();

        Assert.Single(app.Dialogs.Confirmations);
        Assert.Same(editor, app.Navigation.Current);
    }

    [Fact]
    public async Task Leaving_with_unsaved_changes_discards_them_when_the_user_agrees()
    {
        var item = TestData.Item();
        using var app = new TestApp(item);
        await app.NavigateToAsync<ItemListViewModel>();
        var editor = await app.NavigateToAsync<ItemEditorViewModel>(item.Id);
        editor.Name = "Changed";

        await editor.CancelCommand.ExecuteAsync();

        Assert.IsType<ItemListViewModel>(app.Navigation.Current);
        Assert.Equal(item.Name, (await app.Inventory.GetItemAsync(item.Id))!.Name);
    }

    [Fact]
    public async Task Opening_an_item_that_no_longer_exists_shows_an_error_and_goes_back()
    {
        using var app = new TestApp();
        await app.NavigateToAsync<ItemListViewModel>();

        await app.Navigation.NavigateToAsync<ItemEditorViewModel>(Guid.NewGuid());

        Assert.Single(app.Dialogs.Errors);
        Assert.IsType<ItemListViewModel>(app.Navigation.Current);
    }
}
