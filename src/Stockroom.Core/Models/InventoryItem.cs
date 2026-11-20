using System.Text.Json.Serialization;

namespace Stockroom.Core.Models;

/// <summary>
/// A stocked item. Immutable: changes are made with <c>with</c> expressions and saved through
/// <see cref="Services.InventoryService"/>, so a view can never edit stored state by accident.
/// </summary>
public sealed record InventoryItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Sku { get; init; }

    public required string Name { get; init; }

    public string Category { get; init; } = "";

    /// <summary>Only changes through stock movements once the item exists.</summary>
    public int QuantityOnHand { get; init; }

    /// <summary>At or below this quantity the item needs reordering.</summary>
    public int ReorderLevel { get; init; }

    public decimal UnitCost { get; init; }

    [JsonIgnore]
    public bool IsLowStock => QuantityOnHand <= ReorderLevel;

    [JsonIgnore]
    public decimal StockValue => QuantityOnHand * UnitCost;
}
