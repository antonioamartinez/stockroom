namespace Stockroom.Core.Models;

/// <summary>
/// One change to an item's quantity. Positive quantities are stock received, negative are stock issued.
/// The history of movements always explains the current quantity on hand.
/// </summary>
public sealed record StockMovement(Guid ItemId, int Quantity, string Reason, DateTimeOffset Timestamp)
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
