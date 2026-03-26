namespace Clay.Widgets;

/// <summary>
/// Common character filters for <see cref="TextInputStyle.CharFilter"/>.
/// </summary>
public static class TextInputFilters
{
    /// <summary>Digits, minus sign, and decimal point.</summary>
    public static bool NumbersOnly(char ch) => char.IsDigit(ch) || ch is '-' or '.';

    /// <summary>Digits and minus sign only (no decimal point).</summary>
    public static bool IntegersOnly(char ch) => char.IsDigit(ch) || ch == '-';

    /// <summary>Digits only (no sign).</summary>
    public static bool DigitsOnly(char ch) => char.IsDigit(ch);

    /// <summary>Letters only.</summary>
    public static bool AlphaOnly(char ch) => char.IsLetter(ch);

    /// <summary>Letters and digits only.</summary>
    public static bool AlphaNumeric(char ch) => char.IsLetterOrDigit(ch);

    /// <summary>Hex characters (0-9, a-f, A-F).</summary>
    public static bool HexOnly(char ch) => char.IsAsciiHexDigit(ch);

    /// <summary>No whitespace allowed.</summary>
    public static bool NoWhitespace(char ch) => !char.IsWhiteSpace(ch);
}
