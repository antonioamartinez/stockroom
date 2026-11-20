using System.Globalization;

namespace Stockroom.Presentation.ViewModels;

/// <summary>
/// Parses text typed into numeric fields using the user's culture. Editors keep numeric input as
/// strings so a typo becomes a validation error the view model can see, rather than a binding
/// conversion error that never reaches it.
/// </summary>
internal static class InputParsing
{
    public static bool TryParseWholeNumber(string? text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);

    public static bool TryParseAmount(string? text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.CurrentCulture, out value);

    public static string Format(int value) => value.ToString(CultureInfo.CurrentCulture);

    public static string Format(decimal value) => value.ToString("0.00", CultureInfo.CurrentCulture);
}
