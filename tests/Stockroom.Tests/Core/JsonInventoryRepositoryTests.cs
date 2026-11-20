using Stockroom.Core.Models;
using Stockroom.Core.Persistence;
using Stockroom.Tests.TestDoubles;

namespace Stockroom.Tests.Core;

public sealed class JsonInventoryRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "stockroom-tests", Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "inventory.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task Returns_an_empty_inventory_when_the_file_does_not_exist()
    {
        var repository = new JsonInventoryRepository(FilePath);

        Assert.Empty(await repository.GetItemsAsync());
    }

    [Fact]
    public async Task Items_and_movements_survive_a_round_trip_through_the_file()
    {
        var item = TestData.Item(quantity: 4, unitCost: 1.75m);
        var movement = new StockMovement(item.Id, 4, "Delivery", FixedTimeProvider.DefaultNow);
        await new JsonInventoryRepository(FilePath).ApplyMovementAsync(item, movement);

        var reloaded = new JsonInventoryRepository(FilePath);

        Assert.Equal(item, Assert.Single(await reloaded.GetItemsAsync()));
        Assert.Equal(movement, Assert.Single(await reloaded.GetMovementsAsync(item.Id)));
    }

    [Fact]
    public async Task Does_not_leave_a_temporary_file_behind()
    {
        await new JsonInventoryRepository(FilePath).UpsertItemAsync(TestData.Item());

        Assert.Equal([FilePath], Directory.GetFiles(_directory));
    }
}
