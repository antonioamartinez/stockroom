using Stockroom.Core.Models;

namespace Stockroom.Core.Persistence;

/// <summary>Keeps everything in memory. Used by tests and handy for demos.</summary>
public sealed class InMemoryInventoryRepository(IEnumerable<InventoryItem>? seed = null) : DocumentInventoryRepository
{
    private readonly InventoryDocument _seed = new() { Items = seed?.ToList() ?? [] };

    protected override Task<InventoryDocument> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(_seed);

    protected override Task PersistAsync(InventoryDocument document, CancellationToken cancellationToken) => Task.CompletedTask;
}
