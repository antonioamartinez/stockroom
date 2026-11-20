namespace Stockroom.Core.Services;

/// <summary>
/// Outcome of a business operation. Rule violations (a duplicate SKU, issuing more than is on hand)
/// are expected results, not exceptions, so callers handle them as data.
/// </summary>
/// <param name="MemberName">The input the error relates to, when there is one, so a form can show it next to that field.</param>
public sealed record OperationResult<T>(bool Succeeded, T? Value, string? Error, string? MemberName)
{
    public static OperationResult<T> Ok(T value) => new(true, value, null, null);

    public static OperationResult<T> Fail(string error, string? memberName = null) => new(false, default, error, memberName);
}
