using Stockroom.Core.Models;

namespace Stockroom.Tests.TestDoubles;

public static class TestData
{
    public static InventoryItem Item(
        string sku = "BOLT-M6",
        string name = "M6 hex bolt",
        int quantity = 10,
        int reorderLevel = 5,
        decimal unitCost = 0.25m,
        string category = "Fasteners") =>
        new()
        {
            Sku = sku,
            Name = name,
            QuantityOnHand = quantity,
            ReorderLevel = reorderLevel,
            UnitCost = unitCost,
            Category = category,
        };
}
