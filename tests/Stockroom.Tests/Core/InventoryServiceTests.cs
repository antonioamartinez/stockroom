using Stockroom.Core.Models;
using Stockroom.Core.Persistence;
using Stockroom.Core.Services;
using Stockroom.Tests.TestDoubles;

namespace Stockroom.Tests.Core;

public class InventoryServiceTests
{
    private readonly FixedTimeProvider _clock = new();

    private InventoryService CreateService(params InventoryItem[] seed) =>
        new(new InMemoryInventoryRepository(seed), _clock);

    [Fact]
    public async Task SaveItem_rejects_a_duplicate_sku_ignoring_case()
    {
        var service = CreateService(TestData.Item(sku: "BOLT-M6"));

        var result = await service.SaveItemAsync(TestData.Item(sku: "bolt-m6", name: "Another bolt"));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(InventoryItem.Sku), result.MemberName);
    }

    [Fact]
    public async Task SaveItem_trims_text_fields()
    {
        var service = CreateService();

        var result = await service.SaveItemAsync(TestData.Item(sku: "  NUT-M6 ", name: " M6 nut  ", category: " Fasteners "));

        Assert.True(result.Succeeded);
        var saved = await service.GetItemAsync(result.Value!.Id);
        Assert.Equal("NUT-M6", saved!.Sku);
        Assert.Equal("M6 nut", saved.Name);
        Assert.Equal("Fasteners", saved.Category);
    }

    [Fact]
    public async Task SaveItem_records_an_opening_balance_for_a_new_item_with_stock()
    {
        var service = CreateService();
        var item = TestData.Item(quantity: 40);

        await service.SaveItemAsync(item);

        var movement = Assert.Single(await service.GetMovementsAsync(item.Id));
        Assert.Equal(40, movement.Quantity);
        Assert.Equal(InventoryService.OpeningBalanceReason, movement.Reason);
        Assert.Equal(_clock.Now, movement.Timestamp);
    }

    [Fact]
    public async Task SaveItem_keeps_the_stored_quantity_when_updating_an_existing_item()
    {
        var original = TestData.Item(quantity: 12);
        var service = CreateService(original);

        var result = await service.SaveItemAsync(original with { Name = "Renamed", QuantityOnHand = 999 });

        Assert.True(result.Succeeded);
        var saved = await service.GetItemAsync(original.Id);
        Assert.Equal("Renamed", saved!.Name);
        Assert.Equal(12, saved.QuantityOnHand);
    }

    [Fact]
    public async Task AdjustStock_receives_stock_and_records_the_movement()
    {
        var item = TestData.Item(quantity: 3);
        var service = CreateService(item);

        var result = await service.AdjustStockAsync(item.Id, 7, " Delivery ");

        Assert.True(result.Succeeded);
        Assert.Equal(10, result.Value!.QuantityOnHand);
        var movement = Assert.Single(await service.GetMovementsAsync(item.Id));
        Assert.Equal(7, movement.Quantity);
        Assert.Equal("Delivery", movement.Reason);
    }

    [Fact]
    public async Task AdjustStock_will_not_issue_more_than_is_on_hand()
    {
        var item = TestData.Item(quantity: 3);
        var service = CreateService(item);

        var result = await service.AdjustStockAsync(item.Id, -4, "Job 17");

        Assert.False(result.Succeeded);
        Assert.Equal(3, (await service.GetItemAsync(item.Id))!.QuantityOnHand);
        Assert.Empty(await service.GetMovementsAsync(item.Id));
    }

    [Theory]
    [InlineData(0, "Delivery")]
    [InlineData(5, "")]
    [InlineData(5, "   ")]
    public async Task AdjustStock_requires_a_quantity_and_a_reason(int change, string reason)
    {
        var item = TestData.Item();
        var service = CreateService(item);

        var result = await service.AdjustStockAsync(item.Id, change, reason);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AdjustStock_fails_cleanly_for_an_item_that_was_deleted()
    {
        var service = CreateService();

        var result = await service.AdjustStockAsync(Guid.NewGuid(), 1, "Delivery");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task DeleteItem_removes_the_item_and_its_history()
    {
        var item = TestData.Item(quantity: 0);
        var service = CreateService(item);
        await service.AdjustStockAsync(item.Id, 2, "Delivery");

        Assert.True(await service.DeleteItemAsync(item.Id));

        Assert.Null(await service.GetItemAsync(item.Id));
        Assert.Empty(await service.GetMovementsAsync(item.Id));
    }
}
