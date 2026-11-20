using Stockroom.Core.Models;

namespace Stockroom.Core.Persistence;

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryItem>> GetItemsAsync(CancellationToken cancellationToken = default);

    Task<InventoryItem?> GetItemAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpsertItemAsync(InventoryItem item, CancellationToken cancellationToken = default);

    Task<bool> DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovement>> GetMovementsAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>Saves the item's new quantity and the movement that explains it as one write.</summary>
    Task ApplyMovementAsync(InventoryItem updatedItem, StockMovement movement, CancellationToken cancellationToken = default);
}
