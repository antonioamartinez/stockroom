using Stockroom.Core.Models;

namespace Stockroom.Core.Persistence;

/// <summary>Everything the app stores, as one document.</summary>
public sealed class InventoryDocument
{
    public List<InventoryItem> Items { get; init; } = [];

    public List<StockMovement> Movements { get; init; } = [];
}
