using System.Text.Json;

namespace Stockroom.Core.Persistence;

/// <summary>
/// Stores the inventory as one JSON file. Writes go to a temporary file that then replaces the real
/// one, so a crash mid-write leaves the previous file intact instead of a truncated one.
/// </summary>
public sealed class JsonInventoryRepository(string filePath) : DocumentInventoryRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public string FilePath { get; } = filePath;

    protected override async Task<InventoryDocument> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(FilePath))
        {
            return new InventoryDocument();
        }

        await using var stream = File.OpenRead(FilePath);
        return await JsonSerializer.DeserializeAsync<InventoryDocument>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false)
            ?? new InventoryDocument();
    }

    protected override async Task PersistAsync(InventoryDocument document, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(FilePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = FilePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, FilePath, overwrite: true);
    }
}
