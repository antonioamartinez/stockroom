using Stockroom.Core.Models;
using Stockroom.Core.Persistence;

namespace Stockroom.Core.Services;

/// <summary>
/// The business rules. View models call this, never the repository directly, so every rule is
/// enforced in one place no matter which screen triggers the change.
/// </summary>
public sealed class InventoryService(IInventoryRepository repository, TimeProvider clock)
{
    public const string OpeningBalanceReason = "Opening balance";

    public Task<IReadOnlyList<InventoryItem>> GetItemsAsync(CancellationToken cancellationToken = default) =>
        repository.GetItemsAsync(cancellationToken);

    public Task<InventoryItem?> GetItemAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.GetItemAsync(id, cancellationToken);

    public Task<IReadOnlyList<StockMovement>> GetMovementsAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        repository.GetMovementsAsync(itemId, cancellationToken);

    public Task<bool> DeleteItemAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.DeleteItemAsync(id, cancellationToken);

    /// <summary>
    /// Creates or updates an item. SKUs must be unique (ignoring case). For an existing item the
    /// stored quantity is kept: quantity only changes through <see cref="AdjustStockAsync"/>.
    /// A new item's starting quantity is recorded as an opening-balance movement.
    /// </summary>
    public async Task<OperationResult<InventoryItem>> SaveItemAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        var normalized = item with { Sku = item.Sku.Trim(), Name = item.Name.Trim(), Category = item.Category.Trim() };

        var items = await repository.GetItemsAsync(cancellationToken).ConfigureAwait(false);
        if (items.Any(i => i.Id != normalized.Id && string.Equals(i.Sku, normalized.Sku, StringComparison.OrdinalIgnoreCase)))
        {
            return OperationResult<InventoryItem>.Fail($"SKU {normalized.Sku} is already used by another item.", nameof(InventoryItem.Sku));
        }

        var existing = items.FirstOrDefault(i => i.Id == normalized.Id);
        if (existing is not null)
        {
            var updated = normalized with { QuantityOnHand = existing.QuantityOnHand };
            await repository.UpsertItemAsync(updated, cancellationToken).ConfigureAwait(false);
            return OperationResult<InventoryItem>.Ok(updated);
        }

        if (normalized.QuantityOnHand < 0)
        {
            return OperationResult<InventoryItem>.Fail("Starting quantity cannot be negative.", nameof(InventoryItem.QuantityOnHand));
        }

        if (normalized.QuantityOnHand > 0)
        {
            var opening = new StockMovement(normalized.Id, normalized.QuantityOnHand, OpeningBalanceReason, clock.GetUtcNow());
            await repository.ApplyMovementAsync(normalized, opening, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await repository.UpsertItemAsync(normalized, cancellationToken).ConfigureAwait(false);
        }

        return OperationResult<InventoryItem>.Ok(normalized);
    }

    /// <summary>
    /// Receives (positive <paramref name="quantityChange"/>) or issues (negative) stock and records
    /// the movement. Stock on hand can never go below zero.
    /// </summary>
    public async Task<OperationResult<InventoryItem>> AdjustStockAsync(
        Guid itemId, int quantityChange, string reason, CancellationToken cancellationToken = default)
    {
        if (quantityChange == 0)
        {
            return OperationResult<InventoryItem>.Fail("Enter a quantity other than zero.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return OperationResult<InventoryItem>.Fail("Give a reason for the adjustment.");
        }

        var item = await repository.GetItemAsync(itemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            return OperationResult<InventoryItem>.Fail("This item no longer exists.");
        }

        if (item.QuantityOnHand + quantityChange < 0)
        {
            return OperationResult<InventoryItem>.Fail(
                $"Only {item.QuantityOnHand} on hand, so {-quantityChange} cannot be issued.");
        }

        var updated = item with { QuantityOnHand = item.QuantityOnHand + quantityChange };
        var movement = new StockMovement(itemId, quantityChange, reason.Trim(), clock.GetUtcNow());
        await repository.ApplyMovementAsync(updated, movement, cancellationToken).ConfigureAwait(false);
        return OperationResult<InventoryItem>.Ok(updated);
    }
}
