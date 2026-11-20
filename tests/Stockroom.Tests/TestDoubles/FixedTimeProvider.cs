namespace Stockroom.Tests.TestDoubles;

/// <summary>A clock that always returns the same moment, so timestamps can be asserted exactly.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    public FixedTimeProvider() : this(DefaultNow)
    {
    }

    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
