using Stockroom.Core.Models;
using Stockroom.Core.Services;

namespace Stockroom.Wpf;

/// <summary>A few workshop items so the app has something to show on first run.</summary>
internal static class SampleData
{
    public static async Task SeedAsync(InventoryService inventory)
    {
        InventoryItem[] items =
        [
            new() { Sku = "BOLT-M6-30", Name = "M6 x 30 hex bolt", Category = "Fasteners", QuantityOnHand = 240, ReorderLevel = 50, UnitCost = 0.18m },
            new() { Sku = "NUT-M6", Name = "M6 nyloc nut", Category = "Fasteners", QuantityOnHand = 35, ReorderLevel = 50, UnitCost = 0.09m },
            new() { Sku = "GLUE-PVA-500", Name = "PVA wood glue, 500 ml", Category = "Adhesives", QuantityOnHand = 6, ReorderLevel = 2, UnitCost = 7.40m },
            new() { Sku = "SAND-120", Name = "Sanding disc, 120 grit", Category = "Abrasives", QuantityOnHand = 4, ReorderLevel = 20, UnitCost = 0.55m },
            new() { Sku = "TAPE-BLU-24", Name = "Painter's tape, 24 mm", Category = "Consumables", QuantityOnHand = 12, ReorderLevel = 6, UnitCost = 4.25m },
            new() { Sku = "BIT-HSS-5", Name = "HSS drill bit, 5 mm", Category = "Tooling", QuantityOnHand = 0, ReorderLevel = 3, UnitCost = 3.10m },
        ];

        foreach (var item in items)
        {
            await inventory.SaveItemAsync(item).ConfigureAwait(false);
        }
    }
}
