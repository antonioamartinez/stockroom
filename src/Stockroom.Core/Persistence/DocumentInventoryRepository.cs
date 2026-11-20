using Stockroom.Core.Models;

namespace Stockroom.Core.Persistence;

/// <summary>
/// Repository logic shared by the in-memory and JSON file stores. Subclasses only decide how the
/// document is loaded and persisted. A single async gate serialises access, so reads never see a
/// half-applied write.
/// </summary>
public abstract class DocumentInventoryRepository : IInventoryRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private InventoryDocument? _document;

    protected abstract Task<InventoryDocument> LoadAsync(CancellationToken cancellationToken);

    protected abstract Task PersistAsync(InventoryDocument document, CancellationToken cancellationToken);

    public Task<IReadOnlyList<InventoryItem>> GetItemsAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<InventoryItem>>(
            doc => doc.Items.OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase).ToList(),
            cancellationToken);

    public Task<InventoryItem?> GetItemAsync(Guid id, CancellationToken cancellationToken = default) =>
        ReadAsync(doc => doc.Items.FirstOrDefault(i => i.Id == id), cancellationToken);

    public Task<IReadOnlyList<StockMovement>> GetMovementsAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<StockMovement>>(
            doc => doc.Movements.Where(m => m.ItemId == itemId).OrderByDescending(m => m.Timestamp).ToList(),
            cancellationToken);

    public Task UpsertItemAsync(InventoryItem item, CancellationToken cancellationToken = default) =>
        WriteAsync(doc => Upsert(doc, item), cancellationToken);

    public Task<bool> DeleteItemAsync(Guid id, CancellationToken cancellationToken = default) =>
        WriteAsync(doc =>
        {
            doc.Movements.RemoveAll(m => m.ItemId == id);
            return doc.Items.RemoveAll(i => i.Id == id) > 0;
        }, cancellationToken);

    public Task ApplyMovementAsync(InventoryItem updatedItem, StockMovement movement, CancellationToken cancellationToken = default) =>
        WriteAsync(doc =>
        {
            Upsert(doc, updatedItem);
            doc.Movements.Add(movement);
            return true;
        }, cancellationToken);

    private static bool Upsert(InventoryDocument document, InventoryItem item)
    {
        var index = document.Items.FindIndex(i => i.Id == item.Id);
        if (index >= 0)
        {
            document.Items[index] = item;
        }
        else
        {
            document.Items.Add(item);
        }

        return true;
    }

    private async Task<T> ReadAsync<T>(Func<InventoryDocument, T> read, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _document ??= await LoadAsync(cancellationToken).ConfigureAwait(false);
            return read(_document);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<T> WriteAsync<T>(Func<InventoryDocument, T> write, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _document ??= await LoadAsync(cancellationToken).ConfigureAwait(false);
            var result = write(_document);
            try
            {
                await PersistAsync(_document, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // The in-memory copy now differs from what was stored. Drop it so the next call reloads.
                _document = null;
                throw;
            }

            return result;
        }
        finally
        {
            _gate.Release();
        }
    }
}
